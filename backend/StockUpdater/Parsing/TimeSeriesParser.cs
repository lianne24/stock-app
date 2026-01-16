using System.Globalization;
using System.Text.Json;
using StockUpdater.Models;

namespace StockUpdater.Parsing;

public static class TimeSeriesParser
{
    public static IReadOnlyList<StockPriceRow> Parse(
        string json,
        string symbol,
        Timeframe timeframe,
        int maxDaysBack)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string seriesKey = timeframe switch
        {
            Timeframe.D => "Time Series (Daily)",
            Timeframe.W => "Weekly Time Series",
            Timeframe.M => "Monthly Time Series",
            _ => throw new InvalidOperationException($"Unsupported timeframe: {timeframe}")
        };

        if (!root.TryGetProperty(seriesKey, out var seriesElement) || seriesElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Time series section '{seriesKey}' not found. Response may be rate-limited or malformed.");

        var rows = new List<StockPriceRow>();

        foreach (var dateProp in seriesElement.EnumerateObject())
        {
            if (!DateOnly.TryParse(dateProp.Name, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                continue;

            var ohlcv = dateProp.Value;

            // AlphaVantage keys: "1. open", "2. high", "3. low", "4. close", "5. volume"
            if (!TryGetDecimal(ohlcv, "1. open", out var open) ||
                !TryGetDecimal(ohlcv, "2. high", out var high) ||
                !TryGetDecimal(ohlcv, "3. low", out var low) ||
                !TryGetDecimal(ohlcv, "4. close", out var close) ||
                !TryGetLong(ohlcv, "5. volume", out var volume))
            {
                continue;
            }

            // Light sanity check
            if (high < low) continue;

            rows.Add(new StockPriceRow(symbol, timeframe, date, open, high, low, close, volume));
        }

        // Optional maxDaysBack limit (mostly for Daily backfills)
        if (timeframe == Timeframe.D && maxDaysBack > 0)
        {
            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-maxDaysBack));
            rows = rows.Where(r => r.PriceDate >= cutoff).ToList();
        }

        // Return sorted ascending (nice for downstream and debugging)
        return rows.OrderBy(r => r.PriceDate).ToList();
    }

    private static bool TryGetDecimal(JsonElement obj, string prop, out decimal value)
    {
        value = 0m;
        if (!obj.TryGetProperty(prop, out var el)) return false;

        var s = el.GetString();
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetLong(JsonElement obj, string prop, out long value)
    {
        value = 0L;
        if (!obj.TryGetProperty(prop, out var el)) return false;

        var s = el.GetString();
        return long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}
