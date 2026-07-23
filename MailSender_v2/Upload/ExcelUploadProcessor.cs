using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using ClosedXML.Excel;

namespace MailSender_v2.Upload
{
    internal sealed class ExcelUploadProcessor
    {
        public UploadProcessResult Process(string filePath, ISet<string> existingEmails, ISet<string> blockedEmails)
        {
            existingEmails = existingEmails ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            blockedEmails = blockedEmails ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var result = new UploadProcessResult
            {
                FilePath = filePath,
                UploadedAt = DateTime.Now
            };

            result.Logs.Add($"파일 읽기 시작: {System.IO.Path.GetFileName(filePath)}");

            using (var workbook = new XLWorkbook(filePath))
            {
                result.SheetCount = workbook.Worksheets.Count;
                var worksheet = FindWorksheetWithEmailColumn(workbook) ?? workbook.Worksheet(1);
                result.SheetName = worksheet.Name;

                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    throw new InvalidOperationException("Excel 파일에 데이터가 없습니다.");
                }

                var headerRow = range.FirstRowUsed();
                var headerMap = BuildHeaderMap(headerRow);
                if (!headerMap.TryGetValue("Email", out var emailColumn))
                {
                    throw new InvalidOperationException("이메일 컬럼을 찾을 수 없습니다.");
                }

                var seenInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var dataRows = range.RowsUsed().Skip(1).ToList();
                result.TotalRows = dataRows.Count;
                result.Logs.Add($"시트 로드 완료: {worksheet.Name} ({result.TotalRows:N0} 행)");
                result.Logs.Add("이메일 정규화 및 중복 검사 시작");

                foreach (var row in dataRows)
                {
                    try
                    {
                        var email = GetCellText(row, emailColumn);
                        var normalized = NormalizeEmail(email);

                        if (string.IsNullOrWhiteSpace(normalized))
                        {
                            result.EmptyEmailCount++;
                            continue;
                        }

                        if (!IsValidEmail(normalized))
                        {
                            result.InvalidEmailCount++;
                            continue;
                        }

                        if (!seenInFile.Add(normalized))
                        {
                            result.DuplicateCount++;
                            continue;
                        }

                        if (blockedEmails.Contains(normalized))
                        {
                            result.BlockedCount++;
                            continue;
                        }

                        var recipient = new RecipientUpsertDto
                        {
                            Email = email.Trim(),
                            NormalizedEmail = normalized,
                            NoticeNumber = GetMappedCellText(row, headerMap, "NoticeNumber"),
                            AgencyName = GetMappedCellText(row, headerMap, "AgencyName"),
                            DemandAgencyName = GetMappedCellText(row, headerMap, "DemandAgencyName"),
                            NoticeDate = ParseDate(GetMappedCellText(row, headerMap, "NoticeDate")),
                            NoticeName = GetMappedCellText(row, headerMap, "NoticeName"),
                            ManagerName = GetMappedCellText(row, headerMap, "ManagerName"),
                            Phone = GetMappedCellText(row, headerMap, "Phone"),
                            BudgetAmount = ParseDecimal(GetMappedCellText(row, headerMap, "BudgetAmount")),
                            LastUploadedAt = result.UploadedAt,
                            UpdatedAt = result.UploadedAt
                        };

                        result.RecipientsToUpsert.Add(recipient);
                        if (existingEmails.Contains(normalized))
                        {
                            result.UpdatedCount++;
                        }
                        else
                        {
                            result.InsertedCount++;
                        }
                    }
                    catch
                    {
                        result.ErrorCount++;
                    }
                }
            }

            result.Logs.Add($"처리 완료: 신규 {result.InsertedCount:N0}건, 기존 {result.UpdatedCount:N0}건 업데이트, 제외 {result.EmptyEmailCount + result.InvalidEmailCount + result.BlockedCount:N0}건, 중복 제거 {result.DuplicateCount:N0}건, 오류 {result.ErrorCount:N0}건");
            return result;
        }

        private static IXLWorksheet FindWorksheetWithEmailColumn(XLWorkbook workbook)
        {
            foreach (var worksheet in workbook.Worksheets)
            {
                var range = worksheet.RangeUsed();
                if (range == null)
                {
                    continue;
                }

                var headerMap = BuildHeaderMap(range.FirstRowUsed());
                if (headerMap.ContainsKey("Email"))
                {
                    return worksheet;
                }
            }

            return null;
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLRangeRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim();
                var key = GetCanonicalHeader(header);
                if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                {
                    map.Add(key, cell.Address.ColumnNumber);
                }
            }

            return map;
        }

        private static string GetCanonicalHeader(string header)
        {
            var lower = header.ToLowerInvariant().Replace(" ", "");
            if (lower.Contains("email") || lower.Contains("e-mail") || lower.Contains("이메일") || lower.Contains("메일"))
            {
                return "Email";
            }

            if (lower.Contains("입찰공고번호") || lower.Contains("공고번호") || lower.Contains("noticenumber") || lower.Contains("bidntceno"))
            {
                return "NoticeNumber";
            }

            if (lower.Contains("수요기관") || lower.Contains("demandagency") || lower.Contains("ntcedminsttnm"))
            {
                return "DemandAgencyName";
            }

            if (lower.Contains("공고기관") || lower.Contains("기관명") || lower == "기관" || lower.Contains("agencyname") || lower.Contains("bidntceinsttnm"))
            {
                return "AgencyName";
            }

            if (lower.Contains("기관명") || lower.Contains("공고기관") || lower.Contains("수요기관") || lower == "기관")
            {
                return "AgencyName";
            }

            if (lower.Contains("공고일자") || lower.Contains("공고일") || lower.Contains("noticedate") || lower.Contains("bidntcedt"))
            {
                return "NoticeDate";
            }

            if (lower.Contains("공고명") || lower.Contains("입찰공고명") || lower.Contains("noticename") || lower.Contains("bidntcenm"))
            {
                return "NoticeName";
            }

            if (lower.Contains("담당자") || lower.Contains("manager") || lower.Contains("ofcl"))
            {
                return "ManagerName";
            }

            if (lower.Contains("연락처") || lower.Contains("전화") || lower.Contains("tel") || lower.Contains("phone"))
            {
                return "Phone";
            }

            if (lower.Contains("배정예산") || lower.Contains("추정가격") || lower.Contains("예산") || lower.Contains("budget") || lower.Contains("asignbdgtamt"))
            {
                return "BudgetAmount";
            }

            return null;
        }

        private static string GetMappedCellText(IXLRangeRow row, IDictionary<string, int> headerMap, string key)
        {
            return headerMap.TryGetValue(key, out var column)
                ? GetCellText(row, column)
                : "";
        }

        private static string GetCellText(IXLRangeRow row, int column)
        {
            return row.Cell(column).GetFormattedString().Trim();
        }

        private static string NormalizeEmail(string value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }

        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            try
            {
                var address = new MailAddress(value);
                return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase) &&
                       address.Host.Contains(".");
            }
            catch
            {
                return false;
            }
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var formats = new[] { "yyyy-MM-dd", "yyyy.MM.dd", "yyyy/MM/dd", "yyyyMMdd", "yyyy-MM-dd HH:mm:ss", "yyyy.MM.dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss" };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            {
                return exact.Date;
            }

            if (DateTime.TryParse(value, out var parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value
                .Replace(",", "")
                .Replace("원", "")
                .Trim();

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
