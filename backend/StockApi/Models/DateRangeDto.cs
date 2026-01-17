namespace StockApi.Models;

public sealed record DateRangeDto(
    string Symbol,
    string Timeframe,
    DateOnly MinDate,
    DateOnly MaxDate,
    int RowCount
);
