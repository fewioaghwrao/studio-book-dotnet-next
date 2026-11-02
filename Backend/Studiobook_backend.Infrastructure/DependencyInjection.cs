using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Settings;
using Studiobook_backend.Domain.Entities;
using Studiobook_backend.Infrastructure.Persistence;
using Studiobook_backend.Infrastructure.Reports;
using Studiobook_backend.Infrastructure.Services;

namespace Studiobook_backend.Infrastructure;

/// <summary>
/// Infrastructure 層（DB・外部サービス）の登録。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext(MySQL)
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            ));

        // Application 層からは IAppDbContext として利用する（AppDbContext と同一スコープのインスタンス）
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Settings
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        services.Configure<OpenAiSettings>(configuration.GetSection("OpenAI"));

        // 認証まわり
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // メール
        services.AddScoped<IEmailService, EmailService>();

        // OpenAI
        services.AddHttpClient<IOpenAiRoomSearchClient, OpenAiRoomSearchClient>();
        services.AddHttpClient<IOpenAiReviewAssistClient, OpenAiReviewAssistClient>();

        // 帳票（CSV / PDF）
        QuestPdfSetup.Configure();
        services.AddScoped<IHostSalesCsvExporter, HostSalesCsvExporter>();
        services.AddScoped<IHostSalesInvoicePdfGenerator, HostSalesInvoicePdfGenerator>();

        // Stripe
        services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();

        var stripeSecretKey = configuration["Stripe:SecretKey"];

        if (!string.IsNullOrWhiteSpace(stripeSecretKey))
        {
            Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
        }

        return services;
    }
}
