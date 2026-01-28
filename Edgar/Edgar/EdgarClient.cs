using Edgar.Config;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Edgar.Edgar
{
    public class EdgarClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly int _delayMs;
        private DateTime _lastRequestTime = DateTime.MinValue;

        public EdgarClient(AppSettings settings)
        {
            _delayMs = settings.RequestDelayMs;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            // SEC requires a descriptive User-Agent
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json")
            );
        }

        /// <summary>
        /// GET request with basic rate limiting.
        /// </summary>
        public async Task<string> GetStringAsync(string url, CancellationToken ct = default)
        {
            await EnforceRateLimitAsync(ct);

            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }

        /// <summary>
        /// GET request for binary content (HTML, etc.).
        /// </summary>
        public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
        {
            await EnforceRateLimitAsync(ct);

            using var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync(ct);
        }

        private async Task EnforceRateLimitAsync(CancellationToken ct)
        {
            if (_lastRequestTime != DateTime.MinValue)
            {
                var elapsed = DateTime.UtcNow - _lastRequestTime;
                var remainingDelay = _delayMs - (int)elapsed.TotalMilliseconds;

                if (remainingDelay > 0)
                    await Task.Delay(remainingDelay, ct);
            }

            _lastRequestTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
