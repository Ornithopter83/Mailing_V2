using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender_v2.Config;
using MailSender_v2.Upload;

namespace MailSender_v2.Data
{
    internal sealed class SupabaseUploadRepository
    {
        private readonly SupabaseSettings _settings;

        public SupabaseUploadRepository(SupabaseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<HashSet<string>> GetExistingRecipientEmailsAsync(CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                return await client.GetStringColumnValuesAsync("Recipients", "NormalizedEmail", cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task<HashSet<string>> GetBlockedEmailsAsync(CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                return await client.GetStringColumnValuesAsync("BlockedEmails", "NormalizedEmail", cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task UploadAsync(UploadProcessResult result, CancellationToken cancellationToken)
        {
            using (var client = new SupabaseRestClient(_settings))
            {
                if (result.RecipientsToUpsert.Any())
                {
                    await client.UpsertAsync("Recipients", "NormalizedEmail", result.RecipientsToUpsert, cancellationToken)
                        .ConfigureAwait(false);
                }

                await client.InsertAsync("UploadHistory", result.ToHistory(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
