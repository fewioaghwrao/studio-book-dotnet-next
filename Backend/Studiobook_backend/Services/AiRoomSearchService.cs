using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Ai;

namespace Studiobook_backend.Services
{
    public class AiRoomSearchService
    {
        private readonly AppDbContext _context;
        private readonly OpenAiRoomSearchClient _openAiClient;

        public AiRoomSearchService(
            AppDbContext context,
            OpenAiRoomSearchClient openAiClient)
        {
            _context = context;
            _openAiClient = openAiClient;
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

            var conditions = await _openAiClient.AnalyzeQueryAsync(normalizedQuery);

            var dbQuery = _context.Rooms
                .AsNoTracking()
                .Include(room => room.Reviews)
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

            var keywords = BuildSearchKeywords(conditions, normalizedQuery);

            if (keywords.Count > 0)
            {
                dbQuery = dbQuery.Where(room =>
                    keywords.Any(keyword =>
                        room.Name.Contains(keyword) ||
                        room.Description.Contains(keyword) ||
                        room.Address.Contains(keyword)));
            }

            var rooms = await dbQuery
                .OrderByDescending(room => room.Reviews
                    .Where(review => review.PublicVisible)
                    .Select(review => (double?)review.Score)
                    .Average() ?? 0)
                .ThenBy(room => room.Price)
                .ThenByDescending(room => room.Id)
                .Take(5)
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
                        .Count(review => review.PublicVisible),
                    Reason = ""
                })
                .ToListAsync();

            foreach (var room in rooms)
            {
                room.Reason = BuildReason(room, conditions);
            }

            return new AiRoomSearchResponse
            {
                Query = normalizedQuery,
                InterpretedConditions = conditions,
                Rooms = rooms
            };
        }

        private static List<string> BuildSearchKeywords(
            AiRoomSearchConditionsDto conditions,
            string originalQuery)
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

            if (!string.IsNullOrWhiteSpace(conditions.TimePreference))
            {
                keywords.Add(conditions.TimePreference);
            }

            keywords.AddRange(conditions.Keywords);

            return keywords
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .Take(8)
                .ToList();
        }

        private static string BuildReason(
            AiRoomSearchResultDto room,
            AiRoomSearchConditionsDto conditions)
        {
            var reasons = new List<string>();

            if (!string.IsNullOrWhiteSpace(conditions.Purpose))
            {
                reasons.Add($"{conditions.Purpose}用途に合う可能性があります");
            }

            if (!string.IsNullOrWhiteSpace(conditions.Area) &&
                room.Address.Contains(conditions.Area))
            {
                reasons.Add($"{conditions.Area}エリアの条件に合っています");
            }

            if (conditions.Price.HasValue)
            {
                reasons.Add($"1時間あたり{room.Price:N0}円で予算条件に収まります");
            }

            if (conditions.Capacity.HasValue)
            {
                reasons.Add($"{room.Capacity}人まで利用でき、人数条件に合っています");
            }

            if (!string.IsNullOrWhiteSpace(conditions.Atmosphere))
            {
                reasons.Add($"{conditions.Atmosphere}雰囲気の希望に近い可能性があります");
            }

            if (reasons.Count == 0)
            {
                return "入力された希望条件に関連するスタジオとして候補に入りました。";
            }

            return string.Join("。", reasons.Take(2)) + "。";
        }
    }
}
