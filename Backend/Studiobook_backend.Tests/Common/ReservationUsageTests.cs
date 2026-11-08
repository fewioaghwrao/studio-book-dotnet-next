using Studiobook_backend.Application.Common;

namespace Studiobook_backend.Tests.Common;

public class ReservationUsageTests
{
    private static readonly DateTime Now = new(2026, 5, 10, 15, 0, 0);

    [Theory]
    [InlineData("paid", 2026, 5, 10, 12, true)]      // 決済済み・終了済み → 利用済み
    [InlineData("paid", 2026, 5, 10, 15, true)]      // 終了日時ちょうど → 利用済み
    [InlineData("paid", 2026, 5, 10, 16, false)]     // 決済済み・終了前 → 利用前
    [InlineData("booked", 2026, 5, 9, 12, false)]    // 未決済は終了済みでも利用済みにしない
    [InlineData("canceled", 2026, 5, 9, 12, false)]  // キャンセル
    public void IsUsed_決済済みかつ終了日時が現在以前のときだけtrue(
        string status, int year, int month, int day, int hour, bool expected)
    {
        var endAt = new DateTime(year, month, day, hour, 0, 0);

        Assert.Equal(expected, ReservationUsage.IsUsed(status, endAt, Now));
    }

    [Theory]
    [InlineData("booked", 2026, 5, 9, 12, true)]     // 予約済み（未決済）は見込み
    [InlineData("paid", 2026, 5, 10, 16, true)]      // 決済済み・終了前は見込み
    [InlineData("paid", 2026, 5, 10, 12, false)]     // 決済済み・終了済みは確定（見込みではない）
    [InlineData("canceled", 2026, 5, 20, 12, false)] // キャンセルは含めない
    public void IsUpcoming_予約済みまたは終了前の決済済みのときtrue(
        string status, int year, int month, int day, int hour, bool expected)
    {
        var endAt = new DateTime(year, month, day, hour, 0, 0);

        Assert.Equal(expected, ReservationUsage.IsUpcoming(status, endAt, Now));
    }
}
