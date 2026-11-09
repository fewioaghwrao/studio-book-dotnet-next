namespace Studiobook_backend.Application.Dtos.Reservations;

/// <summary>指定した日時にスタジオを予約できるかどうか。</summary>
public enum RoomAvailabilityStatus
{
    /// <summary>空きあり（予約できる）。</summary>
    Available,

    /// <summary>その時間帯は予約済み。</summary>
    Reserved,

    /// <summary>定休日（その曜日の営業時間がない、または休業日）。</summary>
    Holiday,

    /// <summary>その曜日の営業時間が設定されていない。</summary>
    BusinessHoursNotSet,

    /// <summary>営業時間外の時間が含まれている。</summary>
    OutsideBusinessHours,

    /// <summary>休業期間と重なっている。</summary>
    Closure,

    /// <summary>日をまたいでいる（予約は 1 日単位）。</summary>
    CrossDay,

    /// <summary>開始日時が現在（日本時間）以前。</summary>
    Past
}
