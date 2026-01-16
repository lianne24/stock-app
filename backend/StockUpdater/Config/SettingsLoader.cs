using StockUpdater.Models;

namespace StockUpdater.Config;

public static class SettingsLoader
{
    public static UpdaterSettings LoadFromEnvironment()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY");
        string? conn = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
        string? symbolsRaw = Environment.GetEnvironmentVariable("STOCK_SYMBOLS");
        string? timeframesRaw = Environment.GetEnvironmentVariable("TIMEFRAMES");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Missing env var: ALPHAVANTAGE_API_KEY");

        if (string.IsNullOrWhiteSpace(conn))
            throw new InvalidOperationException("Missing env var: MYSQL_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(symbolsRaw))
            throw new InvalidOperationException("Missing env var: STOCK_SYMBOLS (e.g., AAPL,MSFT,AMZN,GOOGL,TSLA)");

        if (string.IsNullOrWhiteSpace(timeframesRaw))
            throw new InvalidOperationException("Missing env var: TIMEFRAMES (e.g., D or D,W,M)");

        var symbols = symbolsRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToUpperInvariant())
            .Distinct()
            .ToList();

        if (symbols.Count == 0)
            throw new InvalidOperationException("STOCK_SYMBOLS parsed to 0 symbols. Check formatting.");

        var timeframes = timeframesRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseTimeframe)
            .Distinct()
            .ToList();

        if (timeframes.Count == 0)
            throw new InvalidOperationException("TIMEFRAMES parsed to 0 values. Use D, W, M.");

        int maxDaysBack = ReadInt("MAX_DAYS_BACK", 3650);
        int httpTimeout = ReadInt("HTTP_TIMEOUT_SECONDS", 20);
        int retryCount = ReadInt("RETRY_COUNT", 3);
        int requestDelay = ReadInt("REQUEST_DELAY_MS", 0);

        return new UpdaterSettings
        {
            ApiKey = apiKey,
            MySqlConnectionString = conn,
            Symbols = symbols,
            Timeframes = timeframes,
            MaxDaysBack = maxDaysBack,
            HttpTimeoutSeconds = httpTimeout,
            RetryCount = retryCount,
            RequestDelayMs = requestDelay
        };
    }

    private static Timeframe ParseTimeframe(string raw)
    {
        return raw.ToUpperInvariant() switch
        {
            "D" => Timeframe.D,
            "W" => Timeframe.W,
            "M" => Timeframe.M,
            _ => throw new InvalidOperationException($"Invalid timeframe '{raw}'. Allowed: D, W, M.")
        };
    }

    private static int ReadInt(string envVar, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(envVar);
        return int.TryParse(raw, out var val) ? val : defaultValue;
    }
}
