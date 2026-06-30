using System.Collections.Generic;
using System.Linq;

namespace MailSender_v2.Data
{
    internal sealed class SupabaseConnectionCheckResult
    {
        public SupabaseConnectionCheckResult(bool isConfigured, IReadOnlyList<SupabaseTableCheckResult> tableResults, string message)
        {
            IsConfigured = isConfigured;
            TableResults = tableResults;
            Message = message;
        }

        public bool IsConfigured { get; }
        public IReadOnlyList<SupabaseTableCheckResult> TableResults { get; }
        public string Message { get; }

        public bool IsSuccessful => IsConfigured && TableResults.Count > 0 && TableResults.All(result => result.IsAccessible);
    }
}
