SELECT symbol, timeframe, COUNT(*) AS rows_count
FROM stock_prices
GROUP BY symbol, timeframe
ORDER BY symbol, timeframe;

SELECT *
FROM stock_prices
WHERE symbol='AAPL' AND timeframe='D'
ORDER BY price_date DESC
;
