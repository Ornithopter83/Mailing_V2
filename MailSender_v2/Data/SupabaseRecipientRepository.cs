using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender_v2.Config;
using MailSender_v2.Mailing;
using Newtonsoft.Json.Linq;

namespace MailSender_v2.Data
{
    internal sealed class SupabaseRecipientRepository
    {
        private readonly SupabaseSettings _settings;

        public SupabaseRecipientRepository(SupabaseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<List<RecipientListItem>> SearchRecipientsAsync(
            bool sortDescending,
            int maxCount,
            bool excludeSent,
            bool excludeBlocked,
            CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                var query = "select=Id,Email,NormalizedEmail,AgencyName,NoticeDate&order=Id.asc";

                var recipients = (await client.GetAllArrayAsync("Recipients", query, cancellationToken)
                        .ConfigureAwait(false))
                    .Select(ToRecipient)
                    .Where(item => !string.IsNullOrWhiteSpace(item.Email))
                    .ToList();
                if (excludeSent)
                {
                    var sentEmails = await GetSentEmailsAsync(client, cancellationToken).ConfigureAwait(false);
                    recipients = recipients
                        .Where(item => !sentEmails.Contains(Normalize(item.NormalizedEmail ?? item.Email)))
                        .ToList();
                }

                if (excludeBlocked)
                {
                    var blockedEmails = await client.GetStringColumnValuesAsync("BlockedEmails", "NormalizedEmail", cancellationToken).ConfigureAwait(false);
                    recipients = recipients
                        .Where(item => !blockedEmails.Contains(Normalize(item.NormalizedEmail ?? item.Email)))
                        .ToList();
                }

                var ordered = sortDescending
                    ? recipients.OrderByDescending(item => item.NoticeDate ?? DateTime.MinValue)
                    : recipients.OrderBy(item => item.NoticeDate ?? DateTime.MaxValue);

                return ordered.Take(maxCount).ToList();
            }
        }

        public async Task<List<DetailedRecipientRow>> SearchDetailedRecipientsAsync(
            DetailedRecipientSearchOptions options,
            CancellationToken cancellationToken)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            using (var client = new SupabaseRestClient(_settings))
            {
                var recipients = (await client.GetAllArrayAsync(
                            "Recipients",
                            "select=Id,Email,NormalizedEmail,AgencyName,NoticeDate,NoticeName,ManagerName,Phone&order=NoticeDate.asc",
                            cancellationToken)
                        .ConfigureAwait(false))
                    .Select(ToDetailedRecipient)
                    .Where(item => !string.IsNullOrWhiteSpace(item.Email))
                    .Where(item => IsInNoticeDateRange(item, options))
                    .ToList();

                var sentMap = await GetSentEmailMapAsync(client, cancellationToken).ConfigureAwait(false);
                var blockedMap = await GetBlockedEmailMapAsync(client, cancellationToken).ConfigureAwait(false);

                foreach (var recipient in recipients)
                {
                    var email = Normalize(recipient.NormalizedEmail ?? recipient.Email);
                    if (blockedMap.TryGetValue(email, out var blockedInfo))
                    {
                        recipient.Status = "차단됨";
                        recipient.BlockedReason = blockedInfo.Reason;
                        recipient.BlockedCreatedAt = blockedInfo.CreatedAt;
                    }
                    else if (sentMap.TryGetValue(email, out var sentInfo))
                    {
                        recipient.Status = "발송완료";
                        recipient.LastProcessedAt = sentInfo.ProcessedAt;
                    }
                    else
                    {
                        recipient.Status = "미발송";
                    }
                }

                return recipients
                    .Where(item => IsIncludedStatus(item.Status, options))
                    .OrderBy(item => item.NoticeDate ?? DateTime.MaxValue)
                    .ThenBy(item => item.Email)
                    .Take(Math.Max(1, options.MaxCount))
                    .ToList();
            }
        }

        public async Task InsertSendHistoriesAsync(IEnumerable<SendHistoryDto> histories, CancellationToken cancellationToken)
        {
            var items = histories.Where(item => !string.IsNullOrWhiteSpace(item.Email)).ToList();
            if (items.Count == 0)
            {
                return;
            }

            using (var client = new SupabaseRestClient(_settings))
            {
                await client.InsertManyAsync("SendHistory", items, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                var sentEmails = await GetSentEmailsAsync(client, cancellationToken).ConfigureAwait(false);
                return new DashboardSummary
                {
                    BlockedEmailCount = await client.GetCountAsync("BlockedEmails", cancellationToken).ConfigureAwait(false),
                    RecipientCount = await client.GetCountAsync("Recipients", cancellationToken).ConfigureAwait(false),
                    SentRecipientCount = sentEmails.Count,
                };
            }
        }

        public async Task DeleteAllSendHistoryAsync(CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                await client.DeleteAsync("SendHistory", "Id=not.is.null", cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<HashSet<string>> GetSentEmailsAsync(
            SupabaseRestClient client,
            CancellationToken cancellationToken)
        {
            var query =
                "select=Email,Status" +
                "&Status=in.(Sent,ManuallyMarkedSent)" +
                "&order=Email.asc";

            var array = await client.GetAllArrayAsync("SendHistory", query, cancellationToken)
                .ConfigureAwait(false);

            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in array)
            {
                var email = Normalize(item["Email"]?.ToString());
                if (!string.IsNullOrWhiteSpace(email))
                {
                    values.Add(email);
                }
            }

            return values;
        }

        private static async Task<Dictionary<string, SendHistoryInfo>> GetSentEmailMapAsync(
            SupabaseRestClient client,
            CancellationToken cancellationToken)
        {
            var query =
                "select=Email,Status,ProcessedAt" +
                "&Status=in.(Sent,ManuallyMarkedSent)" +
                "&order=Email.asc";

            var array = await client.GetAllArrayAsync("SendHistory", query, cancellationToken)
                .ConfigureAwait(false);

            var values = new Dictionary<string, SendHistoryInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in array)
            {
                var email = Normalize(item["Email"]?.ToString());
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                DateTime parsedAt;
                DateTime? processedAt = DateTime.TryParse(item["ProcessedAt"]?.ToString(), out parsedAt)
                    ? parsedAt
                    : (DateTime?)null;

                SendHistoryInfo existing;
                if (!values.TryGetValue(email, out existing) ||
                    (processedAt.HasValue && (!existing.ProcessedAt.HasValue || processedAt.Value > existing.ProcessedAt.Value)))
                {
                    values[email] = new SendHistoryInfo
                    {
                        ProcessedAt = processedAt,
                    };
                }
            }

            return values;
        }

        private static async Task<Dictionary<string, BlockedEmailInfo>> GetBlockedEmailMapAsync(
            SupabaseRestClient client,
            CancellationToken cancellationToken)
        {
            var array = await client.GetAllArrayAsync(
                    "BlockedEmails",
                    "select=NormalizedEmail,Reason,CreatedAt&order=NormalizedEmail.asc",
                    cancellationToken)
                .ConfigureAwait(false);

            var values = new Dictionary<string, BlockedEmailInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in array)
            {
                var email = Normalize(item["NormalizedEmail"]?.ToString());
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                DateTime parsedAt;
                values[email] = new BlockedEmailInfo
                {
                    Reason = item["Reason"]?.ToString(),
                    CreatedAt = DateTime.TryParse(item["CreatedAt"]?.ToString(), out parsedAt)
                        ? parsedAt
                        : (DateTime?)null,
                };
            }

            return values;
        }

        private static RecipientListItem ToRecipient(JToken token)
        {
            DateTime parsedDate;
            var noticeDateText = token["NoticeDate"]?.ToString();
            return new RecipientListItem
            {
                Id = token["Id"]?.Value<long>() ?? 0L,
                Email = token["Email"]?.ToString(),
                NormalizedEmail = Normalize(token["NormalizedEmail"]?.ToString() ?? token["Email"]?.ToString()),
                AgencyName = token["AgencyName"]?.ToString(),
                NoticeDate = DateTime.TryParse(noticeDateText, out parsedDate) ? parsedDate : (DateTime?)null,
            };
        }

        private static DetailedRecipientRow ToDetailedRecipient(JToken token)
        {
            DateTime parsedDate;
            var noticeDateText = token["NoticeDate"]?.ToString();
            var email = token["Email"]?.ToString();
            return new DetailedRecipientRow
            {
                Id = token["Id"]?.Value<long>() ?? 0L,
                Email = email,
                NormalizedEmail = Normalize(token["NormalizedEmail"]?.ToString() ?? email),
                AgencyName = token["AgencyName"]?.ToString(),
                NoticeDate = DateTime.TryParse(noticeDateText, out parsedDate) ? parsedDate : (DateTime?)null,
                NoticeName = token["NoticeName"]?.ToString(),
                ManagerName = token["ManagerName"]?.ToString(),
                Phone = token["Phone"]?.ToString(),
                Status = "미발송",
            };
        }

        private static bool IsInNoticeDateRange(DetailedRecipientRow row, DetailedRecipientSearchOptions options)
        {
            if (!options.NoticeDateFrom.HasValue && !options.NoticeDateTo.HasValue)
            {
                return true;
            }

            if (!row.NoticeDate.HasValue)
            {
                return false;
            }

            var date = row.NoticeDate.Value.Date;
            if (options.NoticeDateFrom.HasValue && date < options.NoticeDateFrom.Value.Date)
            {
                return false;
            }

            if (options.NoticeDateTo.HasValue && date > options.NoticeDateTo.Value.Date)
            {
                return false;
            }

            return true;
        }

        private static bool IsIncludedStatus(string status, DetailedRecipientSearchOptions options)
        {
            if (options.IncludesAllStatuses)
            {
                return true;
            }

            return (options.IncludeUnsent && string.Equals(status, "미발송", StringComparison.Ordinal)) ||
                (options.IncludeSent && string.Equals(status, "발송완료", StringComparison.Ordinal)) ||
                (options.IncludeBlocked && string.Equals(status, "차단됨", StringComparison.Ordinal));
        }

        private static string Normalize(string email)
        {
            return (email ?? "").Trim().ToLowerInvariant();
        }

        private sealed class SendHistoryInfo
        {
            public DateTime? ProcessedAt { get; set; }
        }

        private sealed class BlockedEmailInfo
        {
            public string Reason { get; set; }
            public DateTime? CreatedAt { get; set; }
        }
    }
}
