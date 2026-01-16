using StockApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read connection string from environment.
// In Docker: Server=mysql...
// Locally: Server=localhost...
var connString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException("Missing env var: MYSQL_CONNECTION_STRING");
}

// Register repository as singleton (it holds only a string).
builder.Services.AddSingleton(new StockRepository(connString));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// List symbols
app.MapGet("/api/stocks/symbols", async (StockRepository repo, CancellationToken ct) =>
{
    var symbols = await repo.GetSymbolsAsync(ct);
    return Results.Ok(symbols);
});

// Prices endpoint:
// /api/stocks/prices?symbol=AAPL&timeframe=D&from=2025-01-01&to=2025-12-31
app.MapGet("/api/stocks/prices", async (
    string symbol,
    string timeframe,
    string from,
    string to,
    StockRepository repo,
    CancellationToken ct) =>
{
    // Normalize
    symbol = symbol.Trim().ToUpperInvariant();
    timeframe = timeframe.Trim().ToUpperInvariant();

    // Validate timeframe
    if (timeframe is not ("D" or "W" or "M"))
        return Results.BadRequest(new { error = "Invalid timeframe. Allowed: D, W, M." });

    // Validate dates
    if (!DateOnly.TryParse(from, out var fromDate))
        return Results.BadRequest(new { error = "Invalid 'from' date. Use YYYY-MM-DD." });

    if (!DateOnly.TryParse(to, out var toDate))
        return Results.BadRequest(new { error = "Invalid 'to' date. Use YYYY-MM-DD." });

    if (fromDate > toDate)
        return Results.BadRequest(new { error = "'from' must be <= 'to'." });

    // Safety cap (optional): prevent huge payloads accidentally
    // Example: 5 years max for daily
    var maxDays = 3650;
    if (timeframe == "D" && (toDate.DayNumber - fromDate.DayNumber) > maxDays)
        return Results.BadRequest(new { error = $"Date range too large for Daily. Max ~{maxDays} days." });

    var prices = await repo.GetPricesAsync(symbol, timeframe, fromDate, toDate, ct);
    return Results.Ok(prices);
});

app.Run();
