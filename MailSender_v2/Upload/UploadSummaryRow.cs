namespace MailSender_v2.Upload
{
    internal sealed class UploadSummaryRow
    {
        public UploadSummaryRow(string category, string description, int count, string status, string memo)
        {
            Category = category;
            Description = description;
            Count = count;
            Status = status;
            Memo = memo;
        }

        public string Category { get; }
        public string Description { get; }
        public int Count { get; }
        public string Status { get; }
        public string Memo { get; }
    }
}
