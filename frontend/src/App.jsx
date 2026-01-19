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

  // show/hide swing markers
  const [showMarkers, setShowMarkers] = useState(true);

  // Dark mode persisted in localStorage
  const [darkMode, setDarkMode] = useState(() => {
    const saved = localStorage.getItem("darkMode");
    return saved ? saved === "true" : false;
  });

  useEffect(() => {
    localStorage.setItem("darkMode", String(darkMode));
    document.documentElement.dataset.theme = darkMode ? "dark" : "light";
  }, [darkMode]);

  // Load symbols on startup
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

  // Fetch available range when symbol/timeframe changes
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

        // Default: last ~90 days (bounded)
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
          volume: Number(r.volume),
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
    <div className="page">
      <div className="card">
        <header className="header">
          <div>
            <h1 className="title">Stock Candlestick Analyzer</h1>
            <p className="subtitle">
              Select a stock, timeframe, and date range to visualize OHLC data.
            </p>
          </div>

          <button className="toggle" onClick={() => setDarkMode((v) => !v)}>
            {darkMode ? "Light mode" : "Dark mode"}
          </button>
        </header>

        {/* Controls row: Load button next to "To" */}
        <section className="controlsRow">
          <div className="field">
            <label>Symbol</label>
            <select value={symbol} onChange={(e) => setSymbol(e.target.value)}>
              {symbols.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label>Timeframe</label>
            <select value={timeframe} onChange={(e) => setTimeframe(e.target.value)}>
              {TIMEFRAMES.map((t) => (
                <option key={t.value} value={t.value}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>

          <div className="field">
            <label>From</label>
            <input
              type="date"
              value={from}
              min={minDate}
              max={to || maxDate}
              onChange={(e) => setFrom(e.target.value)}
            />
          </div>

          <div className="field">
            <label>To</label>
            <input
              type="date"
              value={to}
              min={from || minDate}
              max={maxDate}
              onChange={(e) => setTo(e.target.value)}
            />
          </div>

          <button className="primary" onClick={onLoadChart} disabled={loading || !symbol || !from || !to}>
            {loading ? "Loading..." : "Load Chart"}
          </button>
        </section>

        {/* Options row: marker toggle moved here */}
        <section className="optionsRow">
          <label className="checkbox">
            <input
              type="checkbox"
              checked={showMarkers}
              onChange={(e) => setShowMarkers(e.target.checked)}
            />
            <span>Show Peaks/Valleys (N=3)</span>
          </label>
        </section>

        {error && (
          <div className="error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <section className="chartWrap">
          {candles.length === 0 ? (
            <div className="empty">
              No data loaded yet. Choose a range and click <strong>Load Chart</strong>.
            </div>
          ) : (
            <CandlestickChart
              data={candles}
              darkMode={darkMode}
              swingWindow={3}
              showMarkers={showMarkers}
            />
          )}
        </section>
      </div>
    </div>
  );
}
