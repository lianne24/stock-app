const API_BASE = "http://localhost:8080";

async function httpGetJson(url) {
  const res = await fetch(url);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HTTP ${res.status}: ${text}`);
  }
  return res.json();
}

export async function fetchSymbols() {
  return httpGetJson(`${API_BASE}/api/stocks/symbols`);
}

// Requires you added: GET /api/stocks/range?symbol=...&timeframe=...
export async function fetchRange(symbol, timeframe) {
  const s = encodeURIComponent(symbol);
  const tf = encodeURIComponent(timeframe);
  return httpGetJson(`${API_BASE}/api/stocks/range?symbol=${s}&timeframe=${tf}`);
}

// GET /api/stocks/prices?symbol=...&timeframe=...&from=YYYY-MM-DD&to=YYYY-MM-DD&limit=...
export async function fetchPrices({ symbol, timeframe, from, to, limit = 1500 }) {
  const s = encodeURIComponent(symbol);
  const tf = encodeURIComponent(timeframe);
  const f = encodeURIComponent(from);
  const t = encodeURIComponent(to);
  const l = encodeURIComponent(limit);

  return httpGetJson(
    `${API_BASE}/api/stocks/prices?symbol=${s}&timeframe=${tf}&from=${f}&to=${t}&limit=${l}`
  );
}
