using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Studiobook_backend.Data;
using Studiobook_backend.Services;
using Studiobook_backend.Services.Interfaces;
using Studiobook_backend.Settings;
using Studiobook_backend.Seeders;
using Studiobook_backend.Entities;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Infrastructure;
using Stripe;
using System.Text;
using ReviewService = Studiobook_backend.Services.ReviewService;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// DbContext(MySQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

// Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                 ?? throw new InvalidOperationException("Jwt settings are missing.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey));

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Cookie から JWT を読む
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["auth_token"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

        var userId = GetCurrentUserId(httpContext.User);

        var query = await TryReadAiSearchQueryAsync(httpContext, cancellationToken);

        var logService = httpContext.RequestServices
            .GetRequiredService<AiSearchLogService>();

        await logService.WriteAsync(
            query: string.IsNullOrWhiteSpace(query)
                ? "[RateLimitRejected] AI検索リクエスト"
                : query,
            ipAddress: ipAddress,
            userId: userId,
            succeeded: false,
            resultCount: 0,
            errorMessage: "AI検索の利用回数が一時的に上限に達しました。"
        );

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var responseBody = JsonSerializer.Serialize(new
        {
            message = "AI検索の利用回数が一時的に上限に達しました。少し時間をおいてから再度お試しください。"
        });

        await httpContext.Response.WriteAsync(responseBody, cancellationToken);
    };

    options.AddPolicy("AiSearchPolicy", httpContext =>
    {
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IVerificationTokenService, VerificationTokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<HomeService>();
builder.Services.AddScoped<RoomSearchService>();
builder.Services.AddScoped<RoomDetailService>();

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));
builder.Services.Configure<OpenAiSettings>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.AddHttpClient<OpenAiRoomSearchClient>();
builder.Services.AddScoped<AiRoomSearchService>();
builder.Services.AddScoped<AiSearchLogService>();

builder.Services.AddHttpClient<OpenAiReviewAssistClient>();
builder.Services.AddScoped<AiReviewAssistService>();

builder.Services.AddScoped<ReservationConfirmService>();
builder.Services.AddScoped<StripeCheckoutService>();
builder.Services.AddScoped<ReservationCompleteService>();
builder.Services.AddScoped<UserReservationService>();
builder.Services.AddScoped<ReviewService>();

builder.Services.AddScoped<HostClosureService>();
builder.Services.AddScoped<HostBusinessHourService>();
builder.Services.AddScoped<HostPriceRuleService>();
builder.Services.AddScoped<HostReservationService>();
builder.Services.AddScoped<HostReviewService>();
builder.Services.AddScoped<HostSalesService>();
builder.Services.AddScoped<HostStatusService>();

builder.Services.AddScoped<AdminStatusService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<AdminRoomService>();
builder.Services.AddScoped<AdminReservationService>();
builder.Services.AddScoped<AdminSettingsService>();
builder.Services.AddScoped<AdminAuditLogService>();
builder.Services.AddScoped<AdminAiSearchLogService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];

if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

var enableSwagger = builder.Configuration.GetValue<bool>("ENABLE_SWAGGER");

if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Text(
@"Studio Book API

- Swagger UI: /swagger
- Health: /health
", "text/plain"));

app.MapControllers();

var enableMigration = builder.Configuration.GetValue<bool>("ENABLE_DB_MIGRATION");
var enableSeed = builder.Configuration.GetValue<bool>("ENABLE_DB_SEED");

if (enableMigration || enableSeed)
{
    using var scope = app.Services.CreateScope();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("StartupDbInit");

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        if (enableMigration)
        {
            logger.LogInformation("Database migration started.");
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");
        }

        if (enableSeed)
        {
            logger.LogInformation("Database seed started.");
            await DemoDataSeeder.SeedAsync(app.Services);
            logger.LogInformation("Database seed completed.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed during startup.");
        throw;
    }
}

app.Run();

static int? GetCurrentUserId(ClaimsPrincipal user)
{
    var value =
        user.FindFirstValue(ClaimTypes.NameIdentifier) ??
        user.FindFirstValue("sub") ??
        user.FindFirstValue("userId") ??
        user.FindFirstValue("id");

    return int.TryParse(value, out var userId)
        ? userId
        : null;
}

static async Task<string?> TryReadAiSearchQueryAsync(
    HttpContext httpContext,
    CancellationToken cancellationToken)
{
    try
    {
        if (!HttpMethods.IsPost(httpContext.Request.Method))
        {
            return null;
        }

        if (!httpContext.Request.Path.StartsWithSegments("/api/ai/room-search"))
        {
            return null;
        }

        httpContext.Request.EnableBuffering();

        using var reader = new StreamReader(
            httpContext.Request.Body,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(cancellationToken);

        httpContext.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        using var document = JsonDocument.Parse(body);

        if (document.RootElement.TryGetProperty("query", out var queryElement))
        {
            var query = queryElement.GetString();

            if (!string.IsNullOrWhiteSpace(query))
            {
                return query.Trim();
            }
        }

        return null;
    }
    catch
    {
        return null;
    }
}