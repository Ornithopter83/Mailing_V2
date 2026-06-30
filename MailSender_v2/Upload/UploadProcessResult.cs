using System;
using System.Collections.Generic;
using System.IO;

namespace MailSender_v2.Upload
{
    internal sealed class UploadProcessResult
    {
        public string FilePath { get; set; }
        public string FileName => string.IsNullOrWhiteSpace(FilePath) ? "" : Path.GetFileName(FilePath);
        public DateTime UploadedAt { get; set; }
        public int SheetCount { get; set; }
        public string SheetName { get; set; }
        public int TotalRows { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int EmptyEmailCount { get; set; }
        public int InvalidEmailCount { get; set; }
        public int DuplicateCount { get; set; }
        public int BlockedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<RecipientUpsertDto> RecipientsToUpsert { get; } = new List<RecipientUpsertDto>();
        public List<string> Logs { get; } = new List<string>();

        public int ProcessedTotal =>
            InsertedCount +
            UpdatedCount +
            EmptyEmailCount +
            InvalidEmailCount +
            DuplicateCount +
            BlockedCount +
            ErrorCount;

        public UploadHistoryDto ToHistory()
        {
            return new UploadHistoryDto
            {
                FileName = FileName,
                FilePath = FilePath,
                SheetName = SheetName,
                TotalRows = TotalRows,
                InsertedCount = InsertedCount,
                UpdatedCount = UpdatedCount,
                EmptyEmailCount = EmptyEmailCount,
                InvalidEmailCount = InvalidEmailCount,
                DuplicateCount = DuplicateCount,
                BlockedCount = BlockedCount,
                ErrorCount = ErrorCount,
                CreatedAt = UploadedAt
            };
        }

        public IReadOnlyList<UploadSummaryRow> CreateSummaryRows()
        {
            return new[]
            {
                new UploadSummaryRow("성공(신규)", "신규 이메일이 DB에 추가되었습니다.", InsertedCount, "성공", "DB에 새로 등록된 이메일 수"),
                new UploadSummaryRow("성공(기존)", "기존 이메일이 업데이트되었습니다.", UpdatedCount, "성공", "담당자/공고 정보 갱신"),
                new UploadSummaryRow("이메일 없음", "이메일 주소가 비어 있거나 null 값입니다.", EmptyEmailCount, "처리됨", "발송 대상에서 제외"),
                new UploadSummaryRow("이메일 형식 오류", "이메일 형식이 올바르지 않습니다.", InvalidEmailCount, "처리됨", "예: abc@, @korea.kr"),
                new UploadSummaryRow("차단 목록 제외", "차단 목록에 포함된 이메일입니다.", BlockedCount, "처리됨", "DB 차단 이메일 기준"),
                new UploadSummaryRow("중복 행 제거", "동일한 이메일이 파일 내에서 중복되었습니다.", DuplicateCount, "처리됨", "중복 제거 후 처리"),
                new UploadSummaryRow("기타 오류", "기타 오류로 처리되지 않은 항목입니다.", ErrorCount, "오류", "로그에서 상세 확인"),
                new UploadSummaryRow("합계", "전체 처리 결과 합계", ProcessedTotal, "-", "")
            };
        }
    }
}
