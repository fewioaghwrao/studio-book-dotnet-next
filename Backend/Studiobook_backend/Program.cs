using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Studiobook_backend.Application;
using Studiobook_backend.Application.Services;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Infrastructure;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Infrastructure.Seeders;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Application 層（ユースケース）/ Infrastructure 層（DB・外部サービス・帳票出力、QuestPDF のフォント登録を含む）の登録
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// フロントエンドURL（メール認証後のリダイレクト先・パスワード再設定メールのリンク）
// 開発環境で未設定の場合のみ http://localhost:3000 を使い、それ以外の環境では未設定なら起動時にエラーにする
builder.Services
    .AddOptions<FrontendSettings>()
    .Bind(builder.Configuration.GetSection(FrontendSettings.SectionName))
    .PostConfigure(settings =>
    {
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) && builder.Environment.IsDevelopment())
        {
            settings.BaseUrl = FrontendSettings.DevelopmentDefaultBaseUrl;
        }
    })
    .Validate(
        settings => settings.HasValidBaseUrl(),
        "Frontend:BaseUrl（環境変数 Frontend__BaseUrl）にフロントエンドの公開URL（http/https）を設定してください。")
    .ValidateOnStart();

// Settings
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// CORS
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
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