using StockUpdater.Clients;
using StockUpdater.Config;
using StockUpdater.Data;
using StockUpdater.Models;
using StockUpdater.Parsing;

var ct = CancellationToken.None;

try
{
    // Load settings from environment variables.
    // This keeps secrets (API keys, DB passwords) out of source code and supports Docker/VM deployments.
    var settings = SettingsLoader.LoadFromEnvironment();

    Console.WriteLine("[INFO] Updater starting...");
    Console.WriteLine($"[INFO] Symbols: {string.Join(", ", settings.Symbols)}");
    Console.WriteLine($"[INFO] Timeframes: {string.Join(", ", settings.Timeframes)}");

    // DB repository provides two key behaviors:
    // 1) GetMaxDate -> enables incremental updates (insert only new dates)
    // 2) UpsertBatch -> idempotent writes (safe to re-run without duplicates)
    var repo = new StockPriceRepository(settings.MySqlConnectionString);

    // HttpClient should be reused (not created per request).
    // Timeout is configurable via env var (HTTP_TIMEOUT_SECONDS).
    using var http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds)
    };

    // AlphaVantageClient wraps:
    // - URL building per timeframe
    // - retry/backoff logic on rate limits
    // - detection of non-data payloads ("Information"/"Note"/"Error Message")
    var client = new AlphaVantageClient(http, settings.ApiKey, settings.RetryCount, settings.RequestDelayMs);

    int totalUpserts = 0;
    int failures = 0;

    // Sequential processing is intentional:
    // Alpha Vantage free tier rate-limits easily; sequential + delay is the simplest stable strategy.
    foreach (var symbol in settings.Symbols)
    {
        foreach (var tf in settings.Timeframes)
        {
            try
            {
                Console.WriteLine($"[INFO] Fetching {symbol}-{tf}...");

                // Query the latest date we already have for this symbol/timeframe.
                // This allows the updater to be incremental (fast) after the initial load.
                var maxDate = await repo.GetMaxDateAsync(symbol, tf, ct);

                // Fetch raw JSON payload from Alpha Vantage (or throw with a helpful reason).
                var json = await client.GetTimeSeriesJsonAsync(symbol, tf, ct);

                // Convert JSON -> normalized domain rows (OHLCV).
                // Parser is responsible for handling Alpha Vantage field names and numeric parsing.
                var rows = TimeSeriesParser.Parse(json, symbol, tf, settings.MaxDaysBack);

                // Incremental filter: only insert records strictly newer than maxDate (if maxDate exists).
                IEnumerable<StockPriceRow> toUpsert = rows;

                if (maxDate is not null)
                    toUpsert = rows.Where(r => r.PriceDate > maxDate.Value);

                var list = toUpsert.ToList();
                Console.WriteLine($"[INFO] {symbol}-{tf}: parsed={rows.Count}, new={list.Count}, maxDate={(maxDate?.ToString() ?? "NULL")}");

                if (list.Count > 0)
                {
                    // Upsert is idempotent due to the UNIQUE(symbol,timeframe,price_date) key in MySQL.
                    var upserted = await repo.UpsertBatchAsync(list, ct);
                    totalUpserts += upserted;
                    Console.WriteLine($"[INFO] {symbol}-{tf}: upserted={upserted}");
                }
                else
                {
                    Console.WriteLine($"[INFO] {symbol}-{tf}: nothing new to insert.");
                }
            }
            catch (Exception ex)
            {
                failures++;
                // Keep processing other symbols even if one fails (partial progress is better than none).
                Console.WriteLine($"[ERROR] {symbol}-{tf} failed: {ex.Message}");
            }

            // Inter-request delay (even on success) to respect free-tier limits.
            // This is the simplest "set and forget" reliability move for scheduled runs.
            if (settings.RequestDelayMs > 0)
                await Task.Delay(settings.RequestDelayMs, ct);
        }
    }

    Console.WriteLine($"[INFO] Run complete. totalUpserts={totalUpserts}, failures={failures}");

    // Return non-zero if any failures so schedulers (Task Scheduler/cron) can detect problems.
    return failures == 0 ? 0 : 1;
}
catch (Exception ex)
{
    // Startup failures include missing env vars or invalid formats.
    Console.WriteLine("[ERROR] Updater failed at startup:");
    Console.WriteLine(ex.ToString());
    return 1;
}
