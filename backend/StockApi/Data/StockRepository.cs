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
        int limit,
        CancellationToken ct)
    {
        const string sql = @"
SELECT symbol, timeframe, price_date, open_price, high_price, low_price, close_price, volume
FROM stock_prices
WHERE symbol = @symbol
  AND timeframe = @timeframe
  AND price_date >= @from
  AND price_date <= @to
ORDER BY price_date ASC 
LIMIT @limit;";

        var results = new List<OhlcvDto>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe);
        cmd.Parameters.AddWithValue("@from", from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@to", to.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var s = reader.GetString("symbol");
            var tf = reader.GetString("timeframe");

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

    // =========================
    // Aggregation for W/M derived from Daily
    // =========================

    /// <summary>
    /// Returns aggregated Weekly (W) or Monthly (M) candles derived from Daily (D) rows.
    /// - Week start = Monday (DATE_SUB(price_date, INTERVAL WEEKDAY(price_date) DAY))
    /// - Month start = first of month (DATE_FORMAT to YYYY-MM-01)
    /// </summary>
    public async Task<IReadOnlyList<OhlcvDto>> GetAggPricesAsync(
        string symbol,
        string timeframe,
        DateOnly from,
        DateOnly to,
        int limit,
        CancellationToken ct)
    {
        if (timeframe is not ("W" or "M"))
            throw new ArgumentException("Aggregation supported only for W or M.", nameof(timeframe));

        // period_start expression and grouping depend on timeframe
        string periodStartExpr = timeframe == "W"
            ? "DATE_SUB(price_date, INTERVAL WEEKDAY(price_date) DAY)"
            : "STR_TO_DATE(DATE_FORMAT(price_date, '%Y-%m-01'), '%Y-%m-%d')";

        // Note:
        // - We compute, for each period, first_date and last_date to get open/close from those days.
        // - high/low/volume are aggregated across the period.
        // - We return timeframe as the requested timeframe ("W" or "M") for the DTO.
        string sql = $@"
SELECT
  @symbol AS symbol,
  @timeframe AS timeframe,
  g.period_start AS price_date,
  o.open_price  AS open_price,
  g.high_price  AS high_price,
  g.low_price   AS low_price,
  c.close_price AS close_price,
  g.volume      AS volume
FROM (
  SELECT
    {periodStartExpr} AS period_start,
    MIN(price_date) AS first_date,
    MAX(price_date) AS last_date,
    MAX(high_price) AS high_price,
    MIN(low_price)  AS low_price,
    SUM(volume)     AS volume
  FROM stock_prices
  WHERE symbol = @symbol
    AND timeframe = 'D'
    AND price_date >= @from
    AND price_date <= @to
  GROUP BY period_start
) g
JOIN stock_prices o
  ON o.symbol = @symbol AND o.timeframe = 'D' AND o.price_date = g.first_date
JOIN stock_prices c
  ON c.symbol = @symbol AND c.timeframe = 'D' AND c.price_date = g.last_date
ORDER BY g.period_start ASC
LIMIT @limit;";

        var results = new List<OhlcvDto>();

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe);
        cmd.Parameters.AddWithValue("@from", from.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@to", to.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("@limit", limit);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var s = reader.GetString("symbol");
            var tf = reader.GetString("timeframe");

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

    /// <summary>
    /// Returns min/max available period start dates for Weekly/Monthly derived from Daily rows.
    /// row_count = number of aggregated periods (weeks/months).
    /// </summary>
    public async Task<DateRangeDto?> GetAggDateRangeAsync(
        string symbol,
        string timeframe,
        CancellationToken ct)
    {
        if (timeframe is not ("W" or "M"))
            throw new ArgumentException("Aggregation supported only for W or M.", nameof(timeframe));

        string periodStartExpr = timeframe == "W"
            ? "DATE_SUB(price_date, INTERVAL WEEKDAY(price_date) DAY)"
            : "STR_TO_DATE(DATE_FORMAT(price_date, '%Y-%m-01'), '%Y-%m-%d')";

        string sql = $@"
SELECT
  @symbol AS symbol,
  @timeframe AS timeframe,
  MIN(period_start) AS min_date,
  MAX(period_start) AS max_date,
  COUNT(*) AS row_count
FROM (
  SELECT DISTINCT {periodStartExpr} AS period_start
  FROM stock_prices
  WHERE symbol = @symbol
    AND timeframe = 'D'
) x;";

        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@symbol", symbol);
        cmd.Parameters.AddWithValue("@timeframe", timeframe);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        // If there are no daily rows, MIN/MAX will be NULL
        if (reader.IsDBNull(reader.GetOrdinal("min_date")) || reader.IsDBNull(reader.GetOrdinal("max_date")))
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
