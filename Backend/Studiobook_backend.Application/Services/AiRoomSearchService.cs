using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Application.Common;
using Studiobook_backend.Application.Dtos.Ai;
using Studiobook_backend.Application.Dtos.Reservations;
using Studiobook_backend.Application.Interfaces;
using Studiobook_backend.Domain.Entities;

namespace Studiobook_backend.Application.Services
{
    /// <summary>
    /// AI スタジオ検索。
    ///
    /// 1. 検索文を AI で条件（エリア・予算・人数・キーワード・利用日時など）に分解する
    /// 2. エリア・予算・人数で絞り込み、キーワードに一致するスタジオを探す。
    ///    1 件もなければキーワード条件を外して探し直す（Relaxed）
    /// 3. 利用日と開始時刻が分かる場合は、その日時の空き状況を判定し、空きのあるスタジオを上位にする
    /// 4. おすすめ理由は、実際に当てはまった条件（住所・予算・定員・一致した語・空き状況・評価）だけから作る
    /// </summary>
    public class AiRoomSearchService
    {
        private const int CandidateLimit = 20;
        private const int ResultLimit = 5;
        private const int DefaultDurationMinutes = 60;
        private const int MaxReasonsInSummary = 3;

        private static readonly CultureInfo JapaneseCulture = CultureInfo.GetCultureInfo("ja-JP");

        private readonly IAppDbContext _context;
        private readonly IOpenAiRoomSearchClient _openAiClient;
        private readonly RoomAvailabilityService _availabilityService;
        private readonly TimeProvider _timeProvider;

        public AiRoomSearchService(
            IAppDbContext context,
            IOpenAiRoomSearchClient openAiClient,
            RoomAvailabilityService? availabilityService = null,
            TimeProvider? timeProvider = null)
        {
            _context = context;
            _openAiClient = openAiClient;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _availabilityService = availabilityService ?? new RoomAvailabilityService(context, _timeProvider);
        }

        public async Task<AiRoomSearchResponse> SearchAsync(string query)
        {
            var normalizedQuery = query.Trim();

            if (string.IsNullOrWhiteSpace(normalizedQuery))
            {
                throw new ArgumentException("検索文を入力してください。");
            }

            if (normalizedQuery.Length > 200)
            {
                throw new ArgumentException("検索文は200文字以内で入力してください。");
            }

            var nowJapan = JapanTime.Now(_timeProvider);

            var conditions = await _openAiClient.AnalyzeQueryAsync(normalizedQuery, nowJapan)
                ?? new AiRoomSearchConditionsDto();
            conditions.Keywords ??= new List<string>();

            var keywords = BuildSearchKeywords(conditions);
            var notices = new List<string>();

            // 2. 候補の取得（キーワード一致 → なければキーワード条件を外す）
            var candidates = await LoadCandidatesAsync(conditions, keywords);
            var relaxed = false;

            if (candidates.Count == 0 && keywords.Count > 0)
            {
                candidates = await LoadCandidatesAsync(conditions, new List<string>());
                relaxed = candidates.Count > 0;

                if (relaxed)
                {
                    notices.Add("入力された語に一致するスタジオがなかったため、エリア・予算・人数の条件だけで探した結果を表示しています。");
                }
            }

            // 3. 指定日時の空き状況
            var slot = ParseRequestedSlot(conditions);
            IReadOnlyDictionary<int, RoomAvailabilityStatus>? availability = null;

            if (slot != null)
            {
                availability = await _availabilityService.CheckAsync(
                    candidates.Select(x => x.Id).ToList(),
                    slot.StartAt,
                    slot.EndAt);

                if (slot.DurationAssumed)
                {
                    notices.Add("利用時間の指定がないため、1時間として空き状況を判定しています。");
                }

                if (slot.StartAt <= nowJapan)
                {
                    notices.Add("指定された日時は過去のため、予約できません。");
                }
            }

            // 空きのあるスタジオを上位に（同じ空き状況の中では評価順を保つ）
            var rooms = candidates
                .Select((room, index) => new { room, index })
                .OrderBy(x => availability != null &&
                              availability.TryGetValue(x.room.Id, out var status) &&
                              status == RoomAvailabilityStatus.Available
                    ? 0
                    : 1)
                .ThenBy(x => x.index)
                .Take(ResultLimit)
                .Select(x => x.room)
                .ToList();

            // 4. 空き状況とおすすめ理由
            foreach (var room in rooms)
            {
                RoomAvailabilityStatus? status = null;

                if (availability != null && availability.TryGetValue(room.Id, out var found))
                {
                    status = found;
                    room.Availability = ToAvailabilityDto(found);
                }

                room.Reasons = BuildReasons(room, conditions, keywords, slot, status, relaxed);
                room.Reason = ToReasonSummary(room.Reasons);
            }

            return new AiRoomSearchResponse
            {
                Query = normalizedQuery,
                InterpretedConditions = conditions,
                Rooms = rooms,
                RequestedSlot = slot,
                Relaxed = relaxed,
                Notices = notices
            };
        }

        private async Task<List<AiRoomSearchResultDto>> LoadCandidatesAsync(
            AiRoomSearchConditionsDto conditions,
            List<string> keywords)
        {
            var dbQuery = _context.Rooms
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(conditions.Area))
            {
                dbQuery = dbQuery.Where(room =>
                    room.Address.Contains(conditions.Area));
            }

            if (conditions.Price.HasValue && conditions.Price.Value > 0)
            {
                dbQuery = dbQuery.Where(room =>
                    room.Price <= conditions.Price.Value);
            }

            if (conditions.Capacity.HasValue && conditions.Capacity.Value > 0)
            {
                dbQuery = conditions.CapacityCondition switch
                {
                    "max" => dbQuery.Where(room => room.Capacity <= conditions.Capacity.Value),

                    "exact" => dbQuery.Where(room => room.Capacity == conditions.Capacity.Value),

                    _ => dbQuery.Where(room => room.Capacity >= conditions.Capacity.Value)
                };
            }

            if (keywords.Count > 0)
            {
                dbQuery = dbQuery.Where(BuildKeywordPredicate(keywords));
            }

            return await dbQuery
                .OrderByDescending(room => room.Reviews
                    .Where(review => review.PublicVisible)
                    .Select(review => (double?)review.Score)
                    .Average() ?? 0)
                .ThenBy(room => room.Price)
                .ThenByDescending(room => room.Id)
                .Take(CandidateLimit)
                .Select(room => new AiRoomSearchResultDto
                {
                    Id = room.Id,
                    Name = room.Name,
                    ImageName = room.ImageName,
                    Description = room.Description,
                    PostalCode = room.PostalCode,
                    Address = room.Address,
                    Price = room.Price,
                    Capacity = room.Capacity,
                    AverageScore = room.Reviews
                        .Where(review => review.PublicVisible)
                        .Select(review => (double?)review.Score)
                        .Average(),
                    ReviewCount = room.Reviews
                        .Count(review => review.PublicVisible)
                })
                .ToListAsync();
        }

        /// <summary>
        /// 「いずれかのキーワードが名称・説明・住所に含まれる」条件。
        /// keywords.Any(...) はプロバイダによって SQL に変換できないため、
        /// room.Name.Contains(k1) || room.Description.Contains(k1) || ... の OR 条件として組み立てる。
        /// </summary>
        private static Expression<Func<Room, bool>> BuildKeywordPredicate(IReadOnlyList<string> keywords)
        {
            var room = Expression.Parameter(typeof(Room), "room");
            var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;

            Expression? body = null;

            foreach (var keyword in keywords)
            {
                // 定数ではなくクロージャ経由の値にして、SQL のパラメータとして渡す
                var value = Expression.Property(
                    Expression.Constant(new KeywordHolder(keyword)),
                    nameof(KeywordHolder.Value));

                foreach (var propertyName in new[] { nameof(Room.Name), nameof(Room.Description), nameof(Room.Address) })
                {
                    var match = Expression.Call(Expression.Property(room, propertyName), contains, value);
                    body = body == null ? match : Expression.OrElse(body, match);
                }
            }

            return Expression.Lambda<Func<Room, bool>>(body ?? Expression.Constant(true), room);
        }

        private sealed record KeywordHolder(string Value);

        /// <summary>
        /// 名称・説明・住所と照合するキーワード。
        /// 時間帯の希望（TimePreference）は文章ではなく日時の条件なので含めない。
        /// </summary>
        public static List<string> BuildSearchKeywords(AiRoomSearchConditionsDto conditions)
        {
            var keywords = new List<string>();

            if (!string.IsNullOrWhiteSpace(conditions.Keyword))
            {
                keywords.AddRange(
                    conditions.Keyword
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            if (!string.IsNullOrWhiteSpace(conditions.Purpose))
            {
                keywords.Add(conditions.Purpose);
            }

            if (!string.IsNullOrWhiteSpace(conditions.Atmosphere))
            {
                keywords.Add(conditions.Atmosphere);
            }

            keywords.AddRange(conditions.Keywords ?? new List<string>());

            return keywords
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .Take(8)
                .ToList();
        }

        /// <summary>
        /// AI が読み取った日付・時刻から、空き状況を判定する日時を作る。
        /// 日付と開始時刻の両方が分かる場合のみ。終了は 終了時刻 → 利用時間 → 1 時間 の順に決める。
        /// </summary>
        public static AiRequestedSlotDto? ParseRequestedSlot(AiRoomSearchConditionsDto conditions)
        {
            if (!DateOnly.TryParseExact(conditions.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !TryParseTime(conditions.StartTime, out var startTime))
            {
                return null;
            }

            var startAt = date.ToDateTime(startTime);

            if (TryParseTime(conditions.EndTime, out var endTime) && endTime > startTime)
            {
                return new AiRequestedSlotDto
                {
                    StartAt = startAt,
                    EndAt = date.ToDateTime(endTime)
                };
            }

            if (conditions.DurationMinutes is > 0 and <= 24 * 60)
            {
                return new AiRequestedSlotDto
                {
                    StartAt = startAt,
                    EndAt = startAt.AddMinutes(conditions.DurationMinutes.Value)
                };
            }

            return new AiRequestedSlotDto
            {
                StartAt = startAt,
                EndAt = startAt.AddMinutes(DefaultDurationMinutes),
                DurationAssumed = true
            };
        }

        private static bool TryParseTime(string? value, out TimeOnly time)
        {
            return TimeOnly.TryParseExact(
                value,
                new[] { "HH:mm", "H:mm" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out time);
        }

        /// <summary>
        /// おすすめ理由。実際に当てはまった条件だけを、空き状況 → エリア → 一致した語 → 予算 → 人数 → 評価 の順に並べる。
        /// </summary>
        public static List<string> BuildReasons(
            AiRoomSearchResultDto room,
            AiRoomSearchConditionsDto conditions,
            IReadOnlyCollection<string> keywords,
            AiRequestedSlotDto? slot,
            RoomAvailabilityStatus? availability,
            bool relaxed)
        {
            var reasons = new List<string>();

            if (slot != null && availability == RoomAvailabilityStatus.Available)
            {
                reasons.Add($"{FormatSlot(slot)}に空きがあります");
            }

            if (!string.IsNullOrWhiteSpace(conditions.Area) &&
                room.Address.Contains(conditions.Area, StringComparison.Ordinal))
            {
                reasons.Add($"{conditions.Area}にあります");
            }

            var matchedKeywords = keywords
                .Where(keyword =>
                    room.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    room.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();

            if (matchedKeywords.Count > 0)
            {
                reasons.Add($"名称・説明に{string.Join("", matchedKeywords.Select(x => $"「{x}」"))}を含みます");
            }

            if (conditions.Price is > 0 && room.Price <= conditions.Price.Value)
            {
                reasons.Add($"1時間{room.Price:N0}円で、予算（{conditions.Price.Value:N0}円以内）に収まります");
            }

            if (conditions.Capacity is > 0)
            {
                var capacity = conditions.Capacity.Value;

                var capacityReason = conditions.CapacityCondition switch
                {
                    "max" when room.Capacity <= capacity => $"定員{room.Capacity}人で、{capacity}人以下の少人数向けです",
                    "exact" when room.Capacity == capacity => $"定員がちょうど{capacity}人です",
                    "min" or null when room.Capacity >= capacity => $"定員{room.Capacity}人で、{capacity}人で利用できます",
                    _ => null
                };

                if (capacityReason != null)
                {
                    reasons.Add(capacityReason);
                }
            }

            if (room.AverageScore != null && room.ReviewCount > 0)
            {
                reasons.Add($"公開レビューの平均は{room.AverageScore.Value:0.0}（{room.ReviewCount}件）です");
            }

            if (reasons.Count == 0)
            {
                reasons.Add(relaxed
                    ? "入力された語には一致しませんでしたが、ほかの検索条件に近い候補です"
                    : "検索条件に近い候補です");
            }

            return reasons;
        }

        private static string ToReasonSummary(IEnumerable<string> reasons)
        {
            return string.Join("。", reasons.Take(MaxReasonsInSummary)) + "。";
        }

        private static string FormatSlot(AiRequestedSlotDto slot)
        {
            return $"{slot.StartAt.ToString("M/d（ddd）H:mm", JapaneseCulture)}〜{slot.EndAt:H:mm}";
        }

        public static AiRoomAvailabilityDto ToAvailabilityDto(RoomAvailabilityStatus status)
        {
            var (code, label) = status switch
            {
                RoomAvailabilityStatus.Available => ("available", "空きあり"),
                RoomAvailabilityStatus.Reserved => ("reserved", "その時間は予約済み"),
                RoomAvailabilityStatus.Holiday => ("holiday", "定休日"),
                RoomAvailabilityStatus.BusinessHoursNotSet => ("business_hours_not_set", "営業時間が未設定"),
                RoomAvailabilityStatus.OutsideBusinessHours => ("outside_business_hours", "営業時間外"),
                RoomAvailabilityStatus.Closure => ("closure", "休業期間中"),
                RoomAvailabilityStatus.CrossDay => ("cross_day", "日をまたぐため予約できません"),
                RoomAvailabilityStatus.Past => ("past", "過去の日時です"),
                _ => ("unknown", "不明")
            };

            return new AiRoomAvailabilityDto
            {
                Status = code,
                Label = label,
                IsAvailable = status == RoomAvailabilityStatus.Available
            };
        }
    }
}
