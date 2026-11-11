using Studiobook_backend.Application.Common;

namespace Studiobook_backend.Application.Batch;

/// <summary>
/// 1 日 1 回（日本時間の指定時刻）実行するジョブの実行判定。
/// 直近の予定時刻より後に成功していなければ実行する。
/// サーバーの再起動などで予定時刻に動けなかった場合も、次の確認で取り戻せる。
/// </summary>
public static class BatchSchedule
{
    /// <summary>現在以前で最も新しい予定時刻（UTC）。</summary>
    public static DateTime GetLatestScheduledUtc(DateTime nowUtc, TimeOnly runAtJapanTime)
    {
        var nowJapan = JapanTime.FromUtc(nowUtc);
        var todayRun = nowJapan.Date.Add(runAtJapanTime.ToTimeSpan());

        var latestRunJapan = nowJapan >= todayRun
            ? todayRun
            : todayRun.AddDays(-1);

        return JapanTime.ToUtc(latestRunJapan);
    }

    public static bool IsDue(DateTime? lastSucceededUtc, DateTime nowUtc, TimeOnly runAtJapanTime)
    {
        if (lastSucceededUtc == null)
        {
            return true;
        }

        return lastSucceededUtc.Value < GetLatestScheduledUtc(nowUtc, runAtJapanTime);
    }
}
