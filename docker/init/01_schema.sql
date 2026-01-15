USE stockdb;

-- Stores OHLCV per stock symbol and timeframe (D/W/M)
CREATE TABLE IF NOT EXISTS stock_prices (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  symbol VARCHAR(10) NOT NULL,
  timeframe ENUM('D','W','M') NOT NULL,
  price_date DATE NOT NULL,

  open_price  DECIMAL(18,6) NOT NULL,
  high_price  DECIMAL(18,6) NOT NULL,
  low_price   DECIMAL(18,6) NOT NULL,
  close_price DECIMAL(18,6) NOT NULL,
  volume      BIGINT NOT NULL,

  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

  UNIQUE KEY uq_symbol_timeframe_date (symbol, timeframe, price_date),
  INDEX idx_symbol_date (symbol, price_date)
);

-- Track which 5 stocks are “active”
CREATE TABLE IF NOT EXISTS stocks (
  symbol VARCHAR(10) PRIMARY KEY,
  name VARCHAR(100),
  is_active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Initial 5 symbols 
INSERT INTO stocks(symbol, name) VALUES
('AAPL', 'Apple Inc.'),
('MSFT', 'Microsoft Corporation'),
('AMZN', 'Amazon.com, Inc.'),
('GOOGL', 'Alphabet Inc.'),
('TSLA', 'Tesla, Inc.')
ON DUPLICATE KEY UPDATE name = VALUES(name);
