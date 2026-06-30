using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailSender_v2.Config;

namespace MailSender_v2.Data
{
    internal sealed class DatabaseInitializer
    {
        private static readonly string[] RequiredTables =
        {
            "Recipients",
            "UploadHistory",
            "SendHistory",
            "BlockedEmails"
        };

        private readonly SupabaseSettings _settings;

        public DatabaseInitializer(SupabaseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<SupabaseConnectionCheckResult> CheckAsync(CancellationToken cancellationToken)
        {
            if (!_settings.IsConfigured)
            {
                return new SupabaseConnectionCheckResult(
                    false,
                    new List<SupabaseTableCheckResult>(),
                    "SupabaseUrl 또는 SupabaseAnonKey가 설정되지 않았습니다.");
            }

            var tableResults = new List<SupabaseTableCheckResult>();
            using (var client = new SupabaseRestClient(_settings))
            {
                foreach (var tableName in RequiredTables)
                {
                    tableResults.Add(await client.CheckTableAsync(tableName, cancellationToken).ConfigureAwait(false));
                }
            }

            var message = tableResults.TrueForAll(result => result.IsAccessible)
                ? "Supabase 기본 테이블 접근 확인 완료"
                : "Supabase 기본 테이블 중 접근 실패 항목이 있습니다.";

            return new SupabaseConnectionCheckResult(true, tableResults, message);
        }
    }
}
