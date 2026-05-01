using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Studiobook_backend.Data;

namespace Studiobook_backend.Controllers
{
    [ApiController]
    [Authorize(Roles = "Host")]
    [Route("api/host/sales")]
    public class HostSalesExportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HostSalesExportController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{reservationId:int}/items.csv")]
        public async Task<IActionResult> ExportItemsCsv(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservationId &&
                    x.Room.UserId == hostUserId);

            if (reservation == null)
            {
                return NotFound(new { message = "売上明細が見つかりません。" });
            }

            var items = await _context.ReservationChargeItems
                .AsNoTracking()
                .Where(x => x.ReservationId == reservationId)
                .OrderBy(x => x.SliceStart == null)
                .ThenBy(x => x.SliceStart)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var csv = new StringBuilder();

            // Excel文字化け対策：UTF-8 BOM
            csv.Append('\uFEFF');

            csv.AppendLine($"# 予約ID,{Csv(reservation.Id)}");
            csv.AppendLine($"# スタジオ名,{Csv(reservation.Room.Name)}");
            csv.AppendLine($"# 予約者,{Csv(reservation.User.Name)}");
            csv.AppendLine($"# 期間,{Csv($"{FormatDateTime(reservation.StartAt)} 〜 {FormatDateTime(reservation.EndAt)}")}");
            csv.AppendLine($"# 総額(円),{Csv(reservation.Amount)}");
            csv.AppendLine();

            csv.AppendLine(string.Join(",",
                Csv("区分"),
                Csv("明細内容"),
                Csv("開始"),
                Csv("終了"),
                Csv("1時間当たりの値段"),
                Csv("金額(円)")
            ));

            foreach (var item in items)
            {
                csv.AppendLine(string.Join(",",
                    Csv(ToKindLabel(item.Kind)),
                    Csv(item.Description),
                    Csv(FormatDateTime(item.SliceStart)),
                    Csv(FormatDateTime(item.SliceEnd)),
                    Csv(item.UnitRatePerHour),
                    Csv(item.SliceAmount)
                ));
            }

            var fileName = $"reservation-{reservation.Id}-items.csv";

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv; charset=utf-8",
                fileName
            );
        }

        [HttpGet("{reservationId:int}/invoice.pdf")]
        public async Task<IActionResult> ExportInvoicePdf(int reservationId)
        {
            var hostUserId = GetCurrentUserId();

            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(x => x.Room)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservationId &&
                    x.Room.UserId == hostUserId);

            if (reservation == null)
            {
                return NotFound(new { message = "売上明細が見つかりません。" });
            }

            var items = await _context.ReservationChargeItems
                .AsNoTracking()
                .Where(x => x.ReservationId == reservationId)
                .OrderBy(x => x.SliceStart == null)
                .ThenBy(x => x.SliceStart)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("売上明細（請求書）")
                            .FontSize(18)
                            .Bold();

                        column.Item().PaddingTop(8).AlignRight()
                            .Text($"発行日：{DateTime.Now:yyyy/MM/dd}");
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        column.Spacing(16);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100);
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                                columns.RelativeColumn();
                            });

                            AddMetaCell(table, "予約ID", true);
                            AddMetaCell(table, reservation.Id.ToString(), false);
                            AddMetaCell(table, "状態", true);
                            AddMetaCell(table, ToStatusLabel(reservation.Status), false);

                            AddMetaCell(table, "スタジオ", true);
                            AddMetaCell(table, reservation.Room.Name, false);
                            AddMetaCell(table, "予約者", true);
                            AddMetaCell(table, reservation.User.Name, false);

                            AddMetaCell(table, "利用開始", true);
                            AddMetaCell(table, FormatDateTime(reservation.StartAt), false);
                            AddMetaCell(table, "利用終了", true);
                            AddMetaCell(table, FormatDateTime(reservation.EndAt), false);

                            AddMetaCell(table, "総額", true);
                            AddMetaCell(table, $"{reservation.Amount:N0} 円", false);
                            AddMetaCell(table, "", true);
                            AddMetaCell(table, "", false);
                        });

                        column.Item().Text("料金明細").FontSize(14).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(90);
                            });

                            AddHeaderCell(table, "区分");
                            AddHeaderCell(table, "明細内容");
                            AddHeaderCell(table, "適用期間");
                            AddHeaderCell(table, "単価");
                            AddHeaderCell(table, "金額");

                            foreach (var item in items)
                            {
                                AddBodyCell(table, ToKindLabel(item.Kind));
                                AddBodyCell(table, item.Description ?? "-");
                                AddBodyCell(table, FormatPeriod(item.SliceStart, item.SliceEnd));
                                AddBodyCell(table, item.UnitRatePerHour.HasValue
                                    ? $"{item.UnitRatePerHour.Value:N0} 円/時"
                                    : "-");
                                AddBodyCell(table, $"{item.SliceAmount:N0} 円", alignRight: true);
                            }
                        });

                        column.Item().AlignRight().Column(totalColumn =>
                        {
                            totalColumn.Item().Text($"合計　{reservation.Amount:N0} 円")
                                .FontSize(16)
                                .Bold();

                            totalColumn.Item().PaddingTop(4)
                                .Text("本書はシステムにより自動生成されています。")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            });

            var pdf = document.GeneratePdf();

            var fileName = $"reservation-{reservation.Id}-invoice.pdf";

            return File(
                pdf,
                "application/pdf",
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

        private static void AddMetaCell(TableDescriptor table, string text, bool isLabel)
        {
            table.Cell()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(isLabel ? Colors.Grey.Lighten4 : Colors.White)
                .Padding(6)
                .Text(text ?? "")
                .SemiBold();
        }

        private static void AddHeaderCell(TableDescriptor table, string text)
        {
            table.Cell()
                .Background(Colors.Grey.Darken3)
                .Padding(6)
                .Text(text)
                .FontColor(Colors.White)
                .Bold();
        }

        private static void AddBodyCell(TableDescriptor table, string text, bool alignRight = false)
        {
            var cell = table.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(6);

            if (alignRight)
            {
                cell.AlignRight().Text(text ?? "-");
            }
            else
            {
                cell.Text(text ?? "-");
            }
        }

        private static string FormatDateTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy/MM/dd HH:mm") : "";
        }

        private static string FormatPeriod(DateTime? start, DateTime? end)
        {
            if (!start.HasValue || !end.HasValue)
            {
                return "-";
            }

            return $"{FormatDateTime(start)} 〜 {FormatDateTime(end)}";
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

        private static string ToKindLabel(string? kind)
        {
            return kind switch
            {
                "base" => "基本料金",
                "multiplier" => "加算料金",
                "tax" => "消費税",
                "platform_fee" => "手数料",
                "基本料金" => "基本料金",
                "加算料金" => "加算料金",
                _ => kind ?? ""
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