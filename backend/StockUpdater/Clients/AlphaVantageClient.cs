using System.Net;
using System.Text.Json;
using StockUpdater.Models;

namespace StockUpdater.Clients;

public sealed class AlphaVantageClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly int _retryCount;
    private readonly int _requestDelayMs;

    public AlphaVantageClient(HttpClient http, string apiKey, int retryCount, int requestDelayMs)
    {
        _http = http;
        _apiKey = apiKey;
        _retryCount = Math.Max(0, retryCount);
        _requestDelayMs = Math.Max(0, requestDelayMs);
    }

    public async Task<string> GetTimeSeriesJsonAsync(string symbol, Timeframe timeframe, CancellationToken ct)
    {
        string function = timeframe switch
        {
            Timeframe.D => "TIME_SERIES_DAILY",
            Timeframe.W => "TIME_SERIES_WEEKLY",
            Timeframe.M => "TIME_SERIES_MONTHLY",
            _ => throw new InvalidOperationException($"Unsupported timeframe: {timeframe}")
        };

        // Outputsize=full ensures you get more history for backfill.
        // Later you can use compact for faster runs if you only want recent.
        string outputSize = timeframe == Timeframe.D ? "compact" : ""; // weekly/monthly ignore outputsize
        string url = timeframe == Timeframe.D
            ? $"https://www.alphavantage.co/query?function={function}&symbol={Uri.EscapeDataString(symbol)}&outputsize=compact&apikey={Uri.EscapeDataString(_apiKey)}"
            : $"https://www.alphavantage.co/query?function={function}&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(_apiKey)}";


        Exception? last = null;

        for (int attempt = 1; attempt <= _retryCount + 1; attempt++)
        {
            try
            {
                if (_requestDelayMs > 0 && attempt == 1)
                    await Task.Delay(_requestDelayMs, ct);

                using var resp = await _http.GetAsync(url, ct);

                if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    last = new HttpRequestException($"HTTP 429 Too Many Requests for {symbol}-{timeframe}");
                    await BackoffDelayAsync(attempt, ct);
                    continue;
                }

                resp.EnsureSuccessStatusCode();

                string json = await resp.Content.ReadAsStringAsync(ct);

                // Alpha Vantage returns a JSON with "Note" when rate-limited
                if (ContainsAlphaVantageNote(json, out var note))
                {
                    last = new InvalidOperationException($"AlphaVantage rate limit Note: {note}");
                    await BackoffDelayAsync(attempt, ct);
                    continue;
                }

                // Alpha Vantage returns "Error Message" if symbol/function is invalid
                if (ContainsAlphaVantageError(json, out var errorMsg))
                    throw new InvalidOperationException($"AlphaVantage Error Message: {errorMsg}");

                return json;
            }
            catch (Exception ex) when (attempt <= _retryCount)
            {
                last = ex;
                await BackoffDelayAsync(attempt, ct);
            }
        }

        throw new InvalidOperationException($"Failed to fetch {symbol}-{timeframe} after retries.", last);
    }

    private static bool ContainsAlphaVantageNote(string json, out string note)
    {
        note = "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Note", out var n))
            {
                note = n.GetString() ?? "";
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static bool ContainsAlphaVantageError(string json, out string msg)
    {
        msg = "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Error Message", out var e))
            {
                msg = e.GetString() ?? "";
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static Task BackoffDelayAsync(int attempt, CancellationToken ct)
    {
        // Simple exponential backoff: 2s, 4s, 8s... capped at 30s
        int delayMs = Math.Min(30_000, (int)Math.Pow(2, attempt) * 1000);
        return Task.Delay(delayMs, ct);
    }

    private static bool ContainsAlphaVantageInformation(string json, out string info)
    {
        info = "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Information", out var i))
            {
                info = i.GetString() ?? "";
                return true;
            }
        }
        catch { /* ignore */ }
        return false;
    }

}
