using System.Collections.Generic;
using ClosedXML.Excel;

namespace MailSender_v2.Data
{
    internal static class DetailedRecipientExporter
    {
        public static void SaveExcel(IEnumerable<DetailedRecipientRow> rows, string path)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("상세조회");
                var headers = new[]
                {
                    "구분",
                    "입찰공고번호",
                    "공고기관",
                    "수요기관",
                    "공고명",
                    "공고일자",
                    "담당자",
                    "연락처",
                    "이메일",
                    "배정예산",
                };

                for (var index = 0; index < headers.Length; index++)
                {
                    ws.Cell(1, index + 1).Value = headers[index];
                }

                var headerRange = ws.Range(1, 1, 1, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var rowIndex = 2;
                var sequence = 1;
                foreach (var row in rows)
                {
                    ws.Cell(rowIndex, 1).Value = sequence++;
                    ws.Cell(rowIndex, 2).Value = row.NoticeNumber ?? "";
                    ws.Cell(rowIndex, 3).Value = row.AgencyName ?? "";
                    ws.Cell(rowIndex, 4).Value = row.DemandAgencyName ?? "";
                    ws.Cell(rowIndex, 5).Value = row.NoticeName ?? "";
                    ws.Cell(rowIndex, 6).Value = row.NoticeDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    ws.Cell(rowIndex, 7).Value = row.ManagerName ?? "";
                    ws.Cell(rowIndex, 8).Value = row.Phone ?? "";
                    ws.Cell(rowIndex, 9).Value = row.Email ?? "";
                    ws.Cell(rowIndex, 10).Value = row.BudgetAmount ?? 0;
                    if (!row.BudgetAmount.HasValue)
                    {
                        ws.Cell(rowIndex, 10).Value = "";
                    }

                    rowIndex++;
                }

                var usedRange = ws.Range(1, 1, rowIndex - 1, headers.Length);
                usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Column(1).Width = 8;
                ws.Column(2).Width = 18;
                ws.Column(3).Width = 30;
                ws.Column(4).Width = 13;
                ws.Column(5).Width = 45;
                ws.Column(6).Width = 20;
                ws.Column(7).Width = 14;
                ws.Column(8).Width = 16;
                ws.Column(9).Width = 28;
                ws.Column(10).Width = 14;
                ws.Column(10).Style.NumberFormat.Format = "#,##0";
                workbook.SaveAs(path);
            }
        }
    }
}
