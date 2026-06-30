using System;

namespace MailSender_v2.Upload
{
    internal sealed class UploadHistoryDto
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string SheetName { get; set; }
        public int TotalRows { get; set; }
        public int InsertedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int EmptyEmailCount { get; set; }
        public int InvalidEmailCount { get; set; }
        public int DuplicateCount { get; set; }
        public int BlockedCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
