using Edgar.Config;

public sealed class EdgarClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly int _delayMs;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTime _lastRequestUtc = DateTime.MinValue;

    public EdgarClient(AppSettings settings)
    {
        _delayMs = settings.RequestDelayMs;
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            AutomaticDecompression =
                System.Net.DecompressionMethods.GZip |
                System.Net.DecompressionMethods.Deflate
        })
        {
            Timeout = TimeSpan.FromSeconds(60)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/plain");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
    }

    public async Task<string> GetStringAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var response = await SendWithRateLimitAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to GET '{url}'", ex);
        }
    }

    public async Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendWithRateLimitAsync(url, ct);
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task<HttpResponseMessage> SendWithRateLimitAsync(string url, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Request failed for {url}", ex);
            }

            if ((int)response.StatusCode is 429 or 503)
            {
                var retryDelay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 * (attempt + 1));
                response.Dispose();
                await Task.Delay(retryDelay, ct);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return response;
        }

        throw new HttpRequestException($"Failed after retries: {url}");
    }

    public void Dispose()
    {
        _gate.Dispose();
        _httpClient.Dispose();
    }
}
