using Microsoft.Extensions.DependencyInjection.Extensions;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Application.Services;

namespace Studiobook_backend.Application;

/// <summary>
/// Application 層（ユースケース）のサービス登録。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 現在時刻（テストでは差し替え可能）。日本時間への変換は Common/JapanTime を使う
        services.TryAddSingleton(TimeProvider.System);

        // 認証・会員
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IVerificationTokenService, VerificationTokenService>();
        services.AddScoped<PasswordResetService>();

        // ログ
        services.AddScoped<AuditLogService>();
        services.AddScoped<AiSearchLogService>();

        // 公開画面（トップ・検索・詳細・AI）
        services.AddScoped<HomeService>();
        services.AddScoped<RoomSearchService>();
        services.AddScoped<RoomDetailService>();
        services.AddScoped<RoomAvailabilityService>();
        services.AddScoped<AiRoomSearchService>();
        services.AddScoped<AiReviewAssistService>();

        // 予約・レビュー
        services.AddScoped<ReservationConfirmService>();
        services.AddScoped<ReservationCompleteService>();
        services.AddScoped<UserReservationService>();
        services.AddScoped<ReviewService>();

        // ホスト
        services.AddScoped<HostClosureService>();
        services.AddScoped<HostBusinessHourService>();
        services.AddScoped<HostPriceRuleService>();
        services.AddScoped<HostReservationService>();
        services.AddScoped<HostReviewService>();
        services.AddScoped<HostSalesService>();
        services.AddScoped<HostRoomService>();
        services.AddScoped<HostStatusService>();

        // 管理者
        services.AddScoped<AdminStatusService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<AdminRoomService>();
        services.AddScoped<AdminReservationService>();
        services.AddScoped<AdminSettingsService>();
        services.AddScoped<AdminAuditLogService>();
        services.AddScoped<AdminAiSearchLogService>();

        return services;
    }
}
