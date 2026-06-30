using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace MailSender_v2.Upload
{
    internal static class UploadResultExporter
    {
        public static void SaveText(UploadProcessResult result, string path)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"업로드 일시: {result.UploadedAt:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"파일명: {result.FileName}");
            builder.AppendLine($"처리된 시트: {result.SheetName}");
            builder.AppendLine($"전체 데이터 행 수: {result.TotalRows:N0}");
            builder.AppendLine();

            foreach (var row in result.CreateSummaryRows())
            {
                builder.AppendLine($"{row.Category}: {row.Count:N0}");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        public static void SaveExcel(UploadProcessResult result, string path)
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("처리요약");
                ws.Cell(1, 1).Value = "구분";
                ws.Cell(1, 2).Value = "설명";
                ws.Cell(1, 3).Value = "건수";
                ws.Cell(1, 4).Value = "상태";
                ws.Cell(1, 5).Value = "비고";
                ws.Range(1, 1, 1, 5).Style.Font.Bold = true;
                ws.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.AliceBlue;

                var rowIndex = 2;
                foreach (var row in result.CreateSummaryRows())
                {
                    ws.Cell(rowIndex, 1).Value = row.Category;
                    ws.Cell(rowIndex, 2).Value = row.Description;
                    ws.Cell(rowIndex, 3).Value = row.Count;
                    ws.Cell(rowIndex, 4).Value = row.Status;
                    ws.Cell(rowIndex, 5).Value = row.Memo;
                    rowIndex++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(path);
            }
        }
    }
}
