namespace StockApi.Models;

public sealed record OhlcvDto(
    string Symbol,
    string Timeframe,
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
);
