using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host")]
    public class HostSalesCsvController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HostSalesCsvController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("sales.csv")]
        public async Task<IActionResult> ExportSalesCsv(
            [FromQuery] int? roomId,
            [FromQuery] bool onlyWithItems = true)
        {
            var hostUserId = GetCurrentUserId();

            var query = _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .Where(x => x.Room.UserId == hostUserId);

            if (roomId.HasValue)
            {
                query = query.Where(x => x.RoomId == roomId.Value);
            }

            if (onlyWithItems)
            {
                query = query.Where(x => x.Amount > 0);
            }

            var rows = await query
                .OrderByDescending(x => x.StartAt)
                .Select(x => new
                {
                    ReservationId = x.Id,
                    RoomName = x.Room.Name,
                    GuestName = x.User.Name,
                    StartAt = x.StartAt,
                    EndAt = x.EndAt,
                    Amount = x.Amount,
                    Status = x.Status
                })
                .ToListAsync();

            var csv = new StringBuilder();

            // Excel文字化け対策：UTF-8 BOM
            csv.Append('\uFEFF');

            csv.AppendLine(string.Join(",",
                Csv("予約ID"),
                Csv("スタジオ名"),
                Csv("予約者"),
                Csv("予約開始日時"),
                Csv("予約終了日時"),
                Csv("総額(円)"),
                Csv("状態")
            ));

            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",",
                    Csv(row.ReservationId),
                    Csv(row.RoomName),
                    Csv(row.GuestName),
                    Csv(FormatDateTime(row.StartAt)),
                    Csv(FormatDateTime(row.EndAt)),
                    Csv(row.Amount),
                    Csv(ToStatusLabel(row.Status))
                ));
            }

            var fileName = $"host-sales-{DateTime.Now:yyyyMMddHHmmss}.csv";

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv; charset=utf-8",
                fileName
            );
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException("ログインユーザーを取得できません。");
            }

            return userId;
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm");
        }

        private static string ToStatusLabel(string? status)
        {
            return status switch
            {
                "booked" => "予約済み",
                "paid" => "利用済み",
                "canceled" => "キャンセル済み",
                _ => status ?? ""
            };
        }

        private static string Csv(object? value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            var text = value.ToString() ?? "";
            text = text.Replace("\"", "\"\"");

            return $"\"{text}\"";
        }
    }
}