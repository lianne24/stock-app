using MySqlConnector;
using StockApi.Models;

namespace StockApi.Data;

public sealed class StockRepository
{
    private readonly string _connectionString;

    public StockRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<string>> GetSymbolsAsync(CancellationToken ct)
    {
        const string sql = @"
SELECT DISTINCT symbol
FROM stock_prices
ORDER BY symbol;";

        var results = new List<string>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    public async Task<IReadOnlyList<OhlcvDto>> GetPricesAsync(
        string symbol,
        string timeframe,
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        const string sql = @"
SELECT symbol, timeframe, price_date, open_price, high_price, low_price, close_price, volume
FROM stock_prices
WHERE symbol = @symbol
  AND timeframe = @timeframe
  AND price_date >= @from
  AND price_date <= @to
ORDER BY price_date ASC;";

        var results = new List<OhlcvDto>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe);
        cmd.Parameters.AddWithValue("@from", from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@to", to.ToDateTime(TimeOnly.MinValue));

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var s = reader.GetString("symbol");
            var tf = reader.GetString("timeframe");

            // DATE column typically comes back as DateTime
            var dt = reader.GetDateTime("price_date");
            var dateOnly = DateOnly.FromDateTime(dt);

            var open = reader.GetDecimal("open_price");
            var high = reader.GetDecimal("high_price");
            var low = reader.GetDecimal("low_price");
            var close = reader.GetDecimal("close_price");
            var vol = reader.GetInt64("volume");

            results.Add(new OhlcvDto(s, tf, dateOnly, open, high, low, close, vol));
        }

        return results;
    }

    public async Task<DateRangeDto?> GetDateRangeAsync(
        string symbol,
        string timeframe,
        CancellationToken ct)
    {
        const string sql = @"
    SELECT
    symbol,
    timeframe,
    MIN(price_date) AS min_date,
    MAX(price_date) AS max_date,
    COUNT(*) AS row_count
    FROM stock_prices
    WHERE symbol = @symbol AND timeframe = @timeframe
    GROUP BY symbol, timeframe;";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        var s = reader.GetString("symbol");
        var tf = reader.GetString("timeframe");

        var minDt = reader.GetDateTime("min_date");
        var maxDt = reader.GetDateTime("max_date");

        var minDate = DateOnly.FromDateTime(minDt);
        var maxDate = DateOnly.FromDateTime(maxDt);

        var rowCount = reader.GetInt32("row_count");

        return new DateRangeDto(s, tf, minDate, maxDate, rowCount);
    }

}
