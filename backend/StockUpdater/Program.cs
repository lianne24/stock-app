using StockUpdater.Clients;
using StockUpdater.Config;
using StockUpdater.Data;
using StockUpdater.Models;
using StockUpdater.Parsing;

var ct = CancellationToken.None;

try
{
    var settings = SettingsLoader.LoadFromEnvironment();

    Console.WriteLine("[INFO] Updater starting...");
    Console.WriteLine($"[INFO] Symbols: {string.Join(", ", settings.Symbols)}");
    Console.WriteLine($"[INFO] Timeframes: {string.Join(", ", settings.Timeframes)}");

    var repo = new StockPriceRepository(settings.MySqlConnectionString);

    using var http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds)
    };

    var client = new AlphaVantageClient(http, settings.ApiKey, settings.RetryCount, settings.RequestDelayMs);

    int totalUpserts = 0;
    int failures = 0;

    foreach (var symbol in settings.Symbols)
    {
        foreach (var tf in settings.Timeframes)
        {
            try
            {
                Console.WriteLine($"[INFO] Fetching {symbol}-{tf}...");
                var maxDate = await repo.GetMaxDateAsync(symbol, tf, ct);

                var json = await client.GetTimeSeriesJsonAsync(symbol, tf, ct);
                var rows = TimeSeriesParser.Parse(json, symbol, tf, settings.MaxDaysBack);

                IEnumerable<StockPriceRow> toUpsert = rows;

                if (maxDate is not null)
                    toUpsert = rows.Where(r => r.PriceDate > maxDate.Value);

                var list = toUpsert.ToList();
                Console.WriteLine($"[INFO] {symbol}-{tf}: parsed={rows.Count}, new={list.Count}, maxDate={(maxDate?.ToString() ?? "NULL")}");

                if (list.Count > 0)
                {
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
                Console.WriteLine($"[ERROR] {symbol}-{tf} failed: {ex.Message}");
            }
            if (settings.RequestDelayMs > 0)
                await Task.Delay(settings.RequestDelayMs, ct);
        }
    }

    Console.WriteLine($"[INFO] Run complete. totalUpserts={totalUpserts}, failures={failures}");
    return failures == 0 ? 0 : 1;
}
catch (Exception ex)
{
    Console.WriteLine("[ERROR] Updater failed at startup:");
    Console.WriteLine(ex.ToString());
    return 1;
}
