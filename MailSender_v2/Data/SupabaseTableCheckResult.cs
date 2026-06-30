namespace MailSender_v2.Data
{
    internal sealed class SupabaseTableCheckResult
    {
        public SupabaseTableCheckResult(string tableName, bool isAccessible, string message)
        {
            TableName = tableName;
            IsAccessible = isAccessible;
            Message = message;
        }

        public string TableName { get; }
        public bool IsAccessible { get; }
        public string Message { get; }
    }
}
