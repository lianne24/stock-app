import { useEffect, useState } from "react";
import CandlestickChart from "./components/CandlestickChart";
import { fetchPrices, fetchRange, fetchSymbols } from "./api/stocksApi";
import "./App.css";

const TIMEFRAMES = [
  { value: "D", label: "Daily (D)" },
  { value: "W", label: "Weekly (W)" },
  { value: "M", label: "Monthly (M)" },
];

function toIsoDate(dateStr) {
  // Ensures "YYYY-MM-DD"
  return dateStr?.slice(0, 10);
}

export default function App() {
  const [symbols, setSymbols] = useState([]);
  const [symbol, setSymbol] = useState("");
  const [timeframe, setTimeframe] = useState("D");

  const [minDate, setMinDate] = useState("");
  const [maxDate, setMaxDate] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [candles, setCandles] = useState([]);

  // 1) Load symbols on startup
  useEffect(() => {
    (async () => {
      try {
        setError("");
        const list = await fetchSymbols();
        setSymbols(list);
        if (list.length > 0) setSymbol(list[0]);
      } catch (e) {
        setError(String(e.message || e));
      }
    })();
  }, []);

  // 2) Whenever symbol or timeframe changes, fetch available range
  useEffect(() => {
    if (!symbol || !timeframe) return;

    (async () => {
      try {
        setError("");
        const range = await fetchRange(symbol, timeframe);
        const min = toIsoDate(range.minDate);
        const max = toIsoDate(range.maxDate);

        setMinDate(min);
        setMaxDate(max);

        // Default date pickers: last ~90 days if possible, else full range
        setTo(max);
        if (min && max) {
          const maxD = new Date(max + "T00:00:00");
          const fromD = new Date(maxD);
          fromD.setDate(fromD.getDate() - 90);

          const fromIso = fromD.toISOString().slice(0, 10);
          setFrom(fromIso < min ? min : fromIso);
        } else {
          setFrom(min);
        }
      } catch (e) {
        setError(String(e.message || e));
        setMinDate("");
        setMaxDate("");
        setFrom("");
        setTo("");
      }
    })();
  }, [symbol, timeframe]);

  // 3) Load chart data on button click
  async function onLoadChart() {
    if (!symbol || !timeframe || !from || !to) return;

    try {
      setLoading(true);
      setError("");

      const rows = await fetchPrices({ symbol, timeframe, from, to, limit: 5000 });

      const chartData = rows
        .map((r) => ({
          time: toIsoDate(r.date), // "YYYY-MM-DD"
          open: Number(r.open),
          high: Number(r.high),
          low: Number(r.low),
          close: Number(r.close),
        }))
        .filter(
          (c) =>
            c.time &&
            Number.isFinite(c.open) &&
            Number.isFinite(c.high) &&
            Number.isFinite(c.low) &&
            Number.isFinite(c.close)
        );

      setCandles(chartData);
    } catch (e) {
      setError(String(e.message || e));
      setCandles([]);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ minHeight: "100vh", display: "flex", justifyContent: "center" }}>
      <div style={{ width: "100%", maxWidth: 1100, padding: 24 }}>
        <h1 style={{ marginBottom: 8 }}>Stock Candlestick Viewer</h1>
        <p style={{ marginTop: 0, color: "#6b7280" }}>
          Select a stock, timeframe, and date range to visualize OHLC data stored in MySQL.
        </p>

        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr 1fr 1fr auto",
            gap: 12,
            alignItems: "end",
            marginTop: 16,
            marginBottom: 16,
          }}
        >
          <div>
            <label>Symbol</label>
            <select value={symbol} onChange={(e) => setSymbol(e.target.value)} style={{ width: "100%" }}>
              {symbols.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label>Timeframe</label>
            <select value={timeframe} onChange={(e) => setTimeframe(e.target.value)} style={{ width: "100%" }}>
              {TIMEFRAMES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label>From</label>
            <input
              type="date"
              value={from}
              min={minDate}
              max={to || maxDate}
              onChange={(e) => setFrom(e.target.value)}
              style={{ width: "100%" }}
            />
          </div>

          <div>
            <label>To</label>
            <input
              type="date"
              value={to}
              min={from || minDate}
              max={maxDate}
              onChange={(e) => setTo(e.target.value)}
              style={{ width: "100%" }}
            />
          </div>

          <button onClick={onLoadChart} disabled={loading || !symbol || !from || !to}>
            {loading ? "Loading..." : "Load Chart"}
          </button>
        </div>

        {error && (
          <div
            style={{
              marginBottom: 12,
              padding: 12,
              border: "1px solid #fecaca",
              background: "#fef2f2",
              borderRadius: 12,
            }}
          >
            <strong>Error:</strong> {error}
          </div>
        )}

        {/* Centered chart area */}
        <div style={{ marginTop: 16, display: "flex", justifyContent: "center" }}>
          <div style={{ width: "100%", maxWidth: 6000, height: "60vh", minHeight: 340 }}>
            {candles.length === 0 ? (
              <div
                style={{
                  padding: 16,
                  border: "1px dashed #d1d5db",
                  borderRadius: 12,
                  color: "#6b7280",
                }}
              >
                No data loaded yet. Choose a range and click <strong>Load Chart</strong>.
              </div>
            ) : (
              <CandlestickChart data={candles} />
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
