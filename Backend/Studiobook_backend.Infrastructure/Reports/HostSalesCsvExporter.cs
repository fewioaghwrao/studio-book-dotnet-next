using System.Text;
using Studiobook_backend.Application.Dtos.Host;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Reports
{
    /// <summary>
    /// 売上一覧 / 売上明細の CSV 出力。
    /// </summary>
    public class HostSalesCsvExporter : IHostSalesCsvExporter
    {
        public byte[] CreateSalesCsv(IEnumerable<HostSalesCsvRowDto> rows)
        {
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
                    Csv(FormatSalesDateTime(row.StartAt)),
                    Csv(FormatSalesDateTime(row.EndAt)),
                    Csv(row.Amount),
                    Csv(HostSalesLabels.ToStatusLabel(row.Status))
                ));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public byte[] CreateItemsCsv(HostSalesExportDetailDto reservation)
        {
            var items = reservation.Items;

            var csv = new StringBuilder();

            // Excel文字化け対策：UTF-8 BOM
            csv.Append('\uFEFF');

            csv.AppendLine($"# 予約ID,{Csv(reservation.ReservationId)}");
            csv.AppendLine($"# スタジオ名,{Csv(reservation.RoomName)}");
            csv.AppendLine($"# 予約者,{Csv(reservation.GuestName)}");
            csv.AppendLine($"# 期間,{Csv($"{FormatItemDateTime(reservation.StartAt)} 〜 {FormatItemDateTime(reservation.EndAt)}")}");
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
                    Csv(HostSalesLabels.ToKindLabel(item.Kind)),
                    Csv(item.Description),
                    Csv(FormatItemDateTime(item.SliceStart)),
                    Csv(FormatItemDateTime(item.SliceEnd)),
                    Csv(item.UnitRatePerHour),
                    Csv(item.SliceAmount)
                ));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        // 売上一覧 CSV の日時書式
        private static string FormatSalesDateTime(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm");
        }

        // 売上明細 CSV の日時書式
        private static string FormatItemDateTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy/MM/dd HH:mm") : "";
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
