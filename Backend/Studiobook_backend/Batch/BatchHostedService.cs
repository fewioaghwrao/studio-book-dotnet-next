using Microsoft.Extensions.Options;
using Studiobook_backend.Application.Batch;
using Studiobook_backend.Application.Settings;

namespace Studiobook_backend.Batch;

/// <summary>
/// Web アプリ内でバッチを定期実行する（Heroku の Basic dyno で追加料金なしに動かすため）。
///
/// 一定間隔（既定 15 分）で「日本時間の実行時刻（既定 4:00）を過ぎて未実行のジョブ」を実行する。
/// dyno の再起動などで実行時刻を逃しても、次の確認で取り戻す。
/// 複数台で動いても、ジョブ単位の DB ロックと実行記録により重複しない。
/// VPS へ移行した後は Batch__Enabled=false にし、cron から「batch run all」を呼ぶ。
/// </summary>
public sealed class BatchHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BatchSettings _settings;
    private readonly ILogger<BatchHostedService> _logger;

    public BatchHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BatchSettings> settings,
        ILogger<BatchHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Batch scheduler is disabled (Batch:Enabled=false).");
            return;
        }

        _logger.LogInformation(
            "Batch scheduler started. RunAtJapanTime={RunAt}, CheckIntervalMinutes={Interval}",
            _settings.GetRunAtJapanTime().ToString("HH:mm"),
            _settings.CheckIntervalMinutes);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, _settings.InitialDelaySeconds)), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, _settings.CheckIntervalMinutes)));

            do
            {
                await RunDueJobsOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // アプリの停止
        }
    }

    private async Task RunDueJobsOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<BatchJobScheduler>();

            await scheduler.RunDueJobsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // DB に接続できない等。次の確認で再試行する（Web アプリは止めない）
            _logger.LogError(ex, "Batch scheduler check failed.");
        }
    }
}
