using StockUpdater.Config;
using StockUpdater.Data;

var ct = CancellationToken.None;

try
{
    var settings = SettingsLoader.LoadFromEnvironment();

    Console.WriteLine("[INFO] Updater starting...");
    Console.WriteLine($"[INFO] Symbols: {string.Join(", ", settings.Symbols)}");
    Console.WriteLine($"[INFO] Timeframes: {string.Join(", ", settings.Timeframes)}");

    var repo = new StockPriceRepository(settings.MySqlConnectionString);

    // Smoke test DB connectivity: check max date for first symbol/timeframe
    var symbol = settings.Symbols[0];
    var timeframe = settings.Timeframes[0];

    var maxDate = await repo.GetMaxDateAsync(symbol, timeframe, ct);

    Console.WriteLine($"[INFO] DB OK. Max date for {symbol}-{timeframe}: {(maxDate?.ToString() ?? "NULL")}");
    Console.WriteLine("[INFO] Done.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("[ERROR] Updater failed:");
    Console.WriteLine(ex.ToString());
    return 1;
}
