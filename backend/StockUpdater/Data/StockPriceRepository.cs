using MySqlConnector;
using StockUpdater.Models;

namespace StockUpdater.Data;

public sealed class StockPriceRepository
{
    private readonly string _connectionString;

    public StockPriceRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DateOnly?> GetMaxDateAsync(string symbol, Timeframe timeframe, CancellationToken ct)
    {
        const string sql = @"
SELECT MAX(price_date)
FROM stock_prices
WHERE symbol = @symbol AND timeframe = @timeframe;";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe.ToString()); // 'D'/'W'/'M'

        object? result = await cmd.ExecuteScalarAsync(ct);

        if (result is null || result == DBNull.Value)
            return null;

        // MySqlConnector may return DateTime for DATE columns
        if (result is DateTime dt)
            return DateOnly.FromDateTime(dt);

        if (result is string s && DateOnly.TryParse(s, out var dateOnly))
            return dateOnly;

        throw new InvalidOperationException($"Unexpected MAX(price_date) type: {result.GetType().Name}");
    }

    public async Task<int> UpsertBatchAsync(IEnumerable<StockPriceRow> rows, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO stock_prices
(symbol, timeframe, price_date, open_price, high_price, low_price, close_price, volume)
VALUES
(@symbol, @timeframe, @price_date, @open, @high, @low, @close, @volume)
ON DUPLICATE KEY UPDATE
open_price  = VALUES(open_price),
high_price  = VALUES(high_price),
low_price   = VALUES(low_price),
close_price = VALUES(close_price),
volume      = VALUES(volume);";

        var rowList = rows.ToList();
        if (rowList.Count == 0) return 0;

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);

        int executed = 0;

        foreach (var r in rowList)
        {
            await using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@symbol", r.Symbol);
            cmd.Parameters.AddWithValue("@timeframe", r.Timeframe.ToString());
            cmd.Parameters.AddWithValue("@price_date", r.PriceDate.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@open", r.Open);
            cmd.Parameters.AddWithValue("@high", r.High);
            cmd.Parameters.AddWithValue("@low", r.Low);
            cmd.Parameters.AddWithValue("@close", r.Close);
            cmd.Parameters.AddWithValue("@volume", r.Volume);

            await cmd.ExecuteNonQueryAsync(ct);
            executed++;
        }

        await tx.CommitAsync(ct);
        return executed;
    }
}
