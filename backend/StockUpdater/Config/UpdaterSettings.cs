using StockUpdater.Models;

namespace StockUpdater.Config;

public sealed class UpdaterSettings
{
    public required string ApiKey { get; init; }
    public required string MySqlConnectionString { get; init; }
    public required IReadOnlyList<string> Symbols { get; init; }
    public required IReadOnlyList<Timeframe> Timeframes { get; init; }

    public int MaxDaysBack { get; init; } = 3650;
    public int HttpTimeoutSeconds { get; init; } = 20;
    public int RetryCount { get; init; } = 3;
    public int RequestDelayMs { get; init; } = 0;
}
