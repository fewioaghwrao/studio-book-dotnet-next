using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Studiobook_backend.Application.Dtos.Host;
using Studiobook_backend.Application.Interfaces;

namespace Studiobook_backend.Infrastructure.Reports
{
    /// <summary>
    /// QuestPDF による売上明細（請求書）PDF の作成。
    /// フォント（NotoSansJP）とライセンスは QuestPdfSetup で登録します。
    /// </summary>
    public class HostSalesInvoicePdfGenerator : IHostSalesInvoicePdfGenerator
    {
        public byte[] CreateInvoicePdf(HostSalesExportDetailDto reservation)
        {
            var items = reservation.Items;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("NotoSansJP").FontSize(10));

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
                            AddMetaCell(table, reservation.ReservationId.ToString(), false);
                            AddMetaCell(table, "状態", true);
                            AddMetaCell(table, HostSalesLabels.ToStatusLabel(reservation.Status), false);

                            AddMetaCell(table, "スタジオ", true);
                            AddMetaCell(table, reservation.RoomName, false);
                            AddMetaCell(table, "予約者", true);
                            AddMetaCell(table, reservation.GuestName, false);

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
                                AddBodyCell(table, HostSalesLabels.ToKindLabel(item.Kind));
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

            return document.GeneratePdf();
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
    }
}
