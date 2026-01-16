namespace StockUpdater.Models;

public sealed record StockPriceRow(
    string Symbol,
    Timeframe Timeframe,
    DateOnly PriceDate,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);
