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
                    "Id",
                    "이메일",
                    "기관명",
                    "공고일자",
                    "공고명",
                    "담당자",
                    "연락처",
                    "상태",
                    "최근 발송처리일",
                    "차단사유",
                    "차단등록일",
                };

                for (var index = 0; index < headers.Length; index++)
                {
                    ws.Cell(1, index + 1).Value = headers[index];
                }

                ws.Range(1, 1, 1, headers.Length).Style.Font.Bold = true;
                ws.Range(1, 1, 1, headers.Length).Style.Fill.BackgroundColor = XLColor.AliceBlue;

                var rowIndex = 2;
                foreach (var row in rows)
                {
                    ws.Cell(rowIndex, 1).Value = row.Id;
                    ws.Cell(rowIndex, 2).Value = row.Email ?? "";
                    ws.Cell(rowIndex, 3).Value = row.AgencyName ?? "";
                    ws.Cell(rowIndex, 4).Value = row.NoticeDate?.ToString("yyyy-MM-dd") ?? "";
                    ws.Cell(rowIndex, 5).Value = row.NoticeName ?? "";
                    ws.Cell(rowIndex, 6).Value = row.ManagerName ?? "";
                    ws.Cell(rowIndex, 7).Value = row.Phone ?? "";
                    ws.Cell(rowIndex, 8).Value = row.Status ?? "";
                    ws.Cell(rowIndex, 9).Value = row.LastProcessedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    ws.Cell(rowIndex, 10).Value = row.BlockedReason ?? "";
                    ws.Cell(rowIndex, 11).Value = row.BlockedCreatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
                    rowIndex++;
                }

                ws.Columns().AdjustToContents();
                ws.Column(8).Width = 25;
                workbook.SaveAs(path);
            }
        }
    }
}
