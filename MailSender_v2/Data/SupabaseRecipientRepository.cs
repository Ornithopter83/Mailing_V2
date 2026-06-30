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
                var query = "select=Id,Email,NormalizedEmail,AgencyName,NoticeDate&limit=10000";
                var recipients = (await client.GetArrayAsync("Recipients", query, cancellationToken).ConfigureAwait(false))
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

        private static async Task<HashSet<string>> GetSentEmailsAsync(SupabaseRestClient client, CancellationToken cancellationToken)
        {
            var array = await client.GetArrayAsync("SendHistory", "select=Email,Status", cancellationToken).ConfigureAwait(false);
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in array)
            {
                var status = item["Status"]?.ToString();
                if (!string.Equals(status, "Sent", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "ManuallyMarkedSent", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var email = Normalize(item["Email"]?.ToString());
                if (!string.IsNullOrWhiteSpace(email))
                {
                    values.Add(email);
                }
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

        private static string Normalize(string email)
        {
            return (email ?? "").Trim().ToLowerInvariant();
        }
    }
}
