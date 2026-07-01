using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailSender_v2.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MailSender_v2.Data
{
    internal sealed class SupabaseRestClient : IDisposable
    {
        private readonly SupabaseSettings _settings;
        private readonly HttpClient _httpClient;

        public SupabaseRestClient(SupabaseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<SupabaseTableCheckResult> CheckTableAsync(string tableName, CancellationToken cancellationToken)
        {
            if (!_settings.IsConfigured)
            {
                return new SupabaseTableCheckResult(tableName, false, "Supabase 설정이 비어 있습니다.");
            }

            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}?select=*&limit=0";
            using (var request = CreateRequest(HttpMethod.Get, requestUri))
            {
                try
                {
                    using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            return new SupabaseTableCheckResult(tableName, true, "접근 가능");
                        }

                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var message = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            message += $": {TrimForLog(body)}";
                        }

                        return new SupabaseTableCheckResult(tableName, false, message);
                    }
                }
                catch (Exception ex)
                {
                    return new SupabaseTableCheckResult(tableName, false, ex.Message);
                }
            }
        }

        public async Task<HashSet<string>> GetStringColumnValuesAsync(string tableName, string columnName, CancellationToken cancellationToken)
        {
            EnsureConfigured();

            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}?select={Uri.EscapeDataString(columnName)}";
            using (var request = CreateRequest(HttpMethod.Get, requestUri))
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                EnsureSuccess(response, body, tableName);

                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var array = JArray.Parse(body);
                foreach (var item in array)
                {
                    var value = item[columnName]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value.Trim().ToLowerInvariant());
                    }
                }

                return values;
            }
        }

        public async Task<JArray> GetArrayAsync(string tableName, string queryString, CancellationToken cancellationToken)
        {
            EnsureConfigured();

            var separator = string.IsNullOrWhiteSpace(queryString) ? "" : "?" + queryString.TrimStart('?');
            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}{separator}";
            using (var request = CreateRequest(HttpMethod.Get, requestUri))
            using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                EnsureSuccess(response, body, tableName);
                return JArray.Parse(body);
            }
        }

        public async Task UpsertAsync<T>(string tableName, string conflictColumn, IEnumerable<T> items, CancellationToken cancellationToken)
        {
            EnsureConfigured();

            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}?on_conflict={Uri.EscapeDataString(conflictColumn)}";
            var json = JsonConvert.SerializeObject(items);
            using (var request = CreateRequest(HttpMethod.Post, requestUri))
            {
                request.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    EnsureSuccess(response, body, tableName);
                }
            }
        }

        public async Task InsertAsync<T>(string tableName, T item, CancellationToken cancellationToken)
        {
            EnsureConfigured();

            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}";
            var json = JsonConvert.SerializeObject(item);
            using (var request = CreateRequest(HttpMethod.Post, requestUri))
            {
                request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    EnsureSuccess(response, body, tableName);
                }
            }
        }

        public async Task InsertManyAsync<T>(string tableName, IEnumerable<T> items, CancellationToken cancellationToken)
        {
            EnsureConfigured();

            var requestUri = $"{_settings.NormalizedUrl}/rest/v1/{tableName}";
            var json = JsonConvert.SerializeObject(items);
            using (var request = CreateRequest(HttpMethod.Post, requestUri))
            {
                request.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                using (var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    EnsureSuccess(response, body, tableName);
                }
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private static string TrimForLog(string value)
        {
            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return value.Length <= 180 ? value : value.Substring(0, 180) + "...";
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
        {
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.TryAddWithoutValidation("apikey", _settings.AnonKey);

            var apiKey = (_settings.AnonKey ?? "").Trim();
            if (LooksLikeJwt(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            return request;
        }

        private static bool LooksLikeJwt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var parts = value.Split('.');
            return parts.Length == 3 && value.StartsWith("eyJ", StringComparison.Ordinal);
        }

        private void EnsureConfigured()
        {
            if (!_settings.IsConfigured)
            {
                throw new InvalidOperationException("Supabase 설정이 비어 있습니다.");
            }
        }

        private static void EnsureSuccess(HttpResponseMessage response, string body, string tableName)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var message = $"{tableName}: {(int)response.StatusCode} {response.ReasonPhrase}";
            if (!string.IsNullOrWhiteSpace(body))
            {
                message += $": {TrimForLog(body)}";
            }

            throw new InvalidOperationException(message);
        }
    }
}
