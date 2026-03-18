using Edgar.Config;

using System.Net;

public sealed class EdgarClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly int _delayMs;
    private readonly TimeSpan _requestTimeout;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public EdgarClient(AppSettings settings)
    {
        _delayMs = settings.RequestDelayMs;
        _requestTimeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);

        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression =
                DecompressionMethods.GZip |
                DecompressionMethods.Deflate,

            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 2
        })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/plain");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
    }

    public async Task<string> GetStringAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendWithRateLimitAsync(url, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendWithRateLimitAsync(url, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<HttpResponseMessage> SendWithRateLimitAsync(string url, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await _gate.WaitAsync(ct);
            try
            {
                var elapsed = DateTime.UtcNow - _lastRequestUtc;
                var remaining = _delayMs - (int)elapsed.TotalMilliseconds;

                if (remaining > 0)
                    await Task.Delay(remaining, ct);

                _lastRequestUtc = DateTime.UtcNow;
            }
            finally
            {
                _gate.Release();
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_requestTimeout);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                if (attempt == 4)
                    throw new TimeoutException($"Request timed out after {_requestTimeout} for {url}");

                await Task.Delay(GetBackoff(attempt), ct);
                continue;
            }
            catch (HttpRequestException) when (attempt < 4)
            {
                await Task.Delay(GetBackoff(attempt), ct);
                continue;
            }

            if ((int)response.StatusCode is 408 or 429 or 500 or 502 or 503 or 504)
            {
                var retryDelay = response.Headers.RetryAfter?.Delta ?? GetBackoff(attempt);
                response.Dispose();

                if (attempt == 4)
                    throw new HttpRequestException($"Failed after retries: {url}");

                await Task.Delay(retryDelay, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        throw new HttpRequestException($"Failed after retries: {url}");
    }

    private static TimeSpan GetBackoff(int attempt)
    {
        var seconds = Math.Min(Math.Pow(2, attempt + 1), 30);
        return TimeSpan.FromSeconds(seconds);
    }

    public void Dispose()
    {
        _gate.Dispose();
        _httpClient.Dispose();
    }
}
