// Base API URL:
// - Local dev: http://localhost:8080/api   (via .env.local)
// - Production (VM): /api                  (via .env.production)
const API_BASE = import.meta.env.VITE_API_BASE?.trim() || "/api";

async function httpGetJson(url) {
  const res = await fetch(url);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HTTP ${res.status}: ${text}`);
  }
  return res.json();
}

// GET /api/stocks/symbols
export async function fetchSymbols() {
  return httpGetJson(`${API_BASE}/stocks/symbols`);
}

// GET /api/stocks/range?symbol=...&timeframe=...
export async function fetchRange(symbol, timeframe) {
  const s = encodeURIComponent(symbol);
  const tf = encodeURIComponent(timeframe);

  return httpGetJson(
    `${API_BASE}/stocks/range?symbol=${s}&timeframe=${tf}`
  );
}

// GET /api/stocks/prices?symbol=...&timeframe=...&from=YYYY-MM-DD&to=YYYY-MM-DD&limit=...
export async function fetchPrices({
  symbol,
  timeframe,
  from,
  to,
  limit = 1500,
}) {
  const params = new URLSearchParams({
    symbol,
    timeframe,
    from,
    to,
    limit: String(limit),
  });

  return httpGetJson(`${API_BASE}/stocks/prices?${params.toString()}`);
}
