using Studiobook_backend.Application.Batch;

namespace Studiobook_backend.Batch;

/// <summary>
/// コマンドからバッチを実行する（Web サーバーは起動しない）。VPS の cron などから使う。
///
///   dotnet Studiobook_backend.dll batch run all            … すべてのジョブを実行
///   dotnet Studiobook_backend.dll batch run log_cleanup    … 指定したジョブだけ実行
///
/// 終了コード：0 = 成功（ほかで実行中のためスキップした場合を含む）、1 = 失敗、2 = 引数の誤り
/// </summary>
public static class BatchCommand
{
    public static bool IsBatchCommand(string[] args)
    {
        return args.Length >= 1 && string.Equals(args[0], "batch", StringComparison.Ordinal);
    }

    public static async Task<int> RunAsync(IServiceProvider services, string[] args)
    {
        using var scope = services.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("BatchCommand");

        var scheduler = scope.ServiceProvider.GetRequiredService<BatchJobScheduler>();

        if (args.Length < 3 || !string.Equals(args[1], "run", StringComparison.Ordinal))
        {
            logger.LogError(
                "Usage: batch run <all|{Jobs}>",
                string.Join("|", scheduler.JobNames));
            return 2;
        }

        var target = args[2];
        var jobNames = string.Equals(target, "all", StringComparison.Ordinal)
            ? scheduler.JobNames
            : new[] { target };

        var exitCode = 0;

        foreach (var jobName in jobNames)
        {
            var status = await scheduler.RunByNameAsync(jobName);

            if (status == null)
            {
                logger.LogError(
                    "Unknown batch job: {Job}. Available: {Jobs}",
                    jobName,
                    string.Join(", ", scheduler.JobNames));
                return 2;
            }

            if (status == BatchRunStatus.Failed)
            {
                exitCode = 1;
            }
        }

        return exitCode;
    }
}
