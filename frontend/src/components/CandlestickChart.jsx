// frontend/src/components/CandlestickChart.jsx
import { useEffect, useMemo, useRef } from "react";
import {
  createChart,
  CrosshairMode,
  CandlestickSeries,
  createSeriesMarkers,
} from "lightweight-charts";

// ---------- swing detection (N=3 by default) ----------
function computeSwingMarkers(data, N, darkMode) {
  if (!Array.isArray(data) || data.length < 2 * N + 1) return [];

  const markers = [];
  const peakColor = darkMode ? "#fca5a5" : "#b91c1c"; // red-ish
  const valleyColor = darkMode ? "#86efac" : "#166534"; // green-ish

  for (let i = N; i < data.length - N; i++) {
    const c = data[i];
    const high = Number(c.high);
    const low = Number(c.low);

    if (!Number.isFinite(high) || !Number.isFinite(low)) continue;

    let isPeak = true;
    let isValley = true;

    for (let k = 1; k <= N; k++) {
      const left = data[i - k];
      const right = data[i + k];

      const leftHigh = Number(left.high);
      const rightHigh = Number(right.high);
      const leftLow = Number(left.low);
      const rightLow = Number(right.low);

      // Peak: strictly greater than all surrounding highs
      if (Number.isFinite(leftHigh) && high <= leftHigh) isPeak = false;
      if (Number.isFinite(rightHigh) && high <= rightHigh) isPeak = false;

      // Valley: strictly lower than all surrounding lows
      if (Number.isFinite(leftLow) && low >= leftLow) isValley = false;
      if (Number.isFinite(rightLow) && low >= rightLow) isValley = false;

      if (!isPeak && !isValley) break;
    }

    if (isPeak) {
      markers.push({
        time: c.time,
        position: "aboveBar",
        shape: "arrowDown",
        color: peakColor,
        text: "P",
      });
    }

    if (isValley) {
      markers.push({
        time: c.time,
        position: "belowBar",
        shape: "arrowUp",
        color: valleyColor,
        text: "V",
      });
    }
  }

  return markers;
}

function formatNumber(n) {
  if (n === null || n === undefined) return "-";
  const x = Number(n);
  return Number.isFinite(x) ? x.toFixed(2) : "-";
}

export default function CandlestickChart({
  data,
  darkMode,
  swingWindow = 3,
  showMarkers = true, // NEW
}) {
  const containerRef = useRef(null);
  const legendRef = useRef(null);

  // NEW: when showMarkers=false -> no markers
  const swingMarkers = useMemo(() => {
    if (!showMarkers) return [];
    return computeSwingMarkers(data, swingWindow, darkMode);
  }, [data, swingWindow, darkMode, showMarkers]);

  useEffect(() => {
    if (!containerRef.current) return;

    const container = containerRef.current;

    const chart = createChart(container, {
      width: container.clientWidth,
      height: container.clientHeight,
      crosshair: { mode: CrosshairMode.Normal },
      rightPriceScale: { borderVisible: true },
      timeScale: { borderVisible: true, timeVisible: true },
    });

    const candleSeries = chart.addSeries(CandlestickSeries, {});
    const markersApi = createSeriesMarkers(candleSeries, []);

    container.__chart = chart;
    container.__candleSeries = candleSeries;
    container.__markersApi = markersApi;

    // Tooltip / crosshair legend
    const onCrosshair = (param) => {
      const legend = legendRef.current;
      if (!legend) return;

      if (!param || !param.time || !param.seriesData) {
        legend.textContent = "Hover chart to see details";
        return;
      }

      const c = param.seriesData.get(candleSeries);
      if (!c) {
        legend.textContent = "Hover chart to see details";
        return;
      }

      const date = typeof param.time === "string" ? param.time : String(param.time);
      legend.textContent =
        `${date}  O:${formatNumber(c.open)}  H:${formatNumber(c.high)}  ` +
        `L:${formatNumber(c.low)}  C:${formatNumber(c.close)}`;
    };

    chart.subscribeCrosshairMove(onCrosshair);

    // Responsive sizing
    const applySize = () => {
      if (!containerRef.current) return;
      chart.applyOptions({
        width: containerRef.current.clientWidth,
        height: containerRef.current.clientHeight,
      });
    };

    applySize();
    chart.timeScale().fitContent();

    let ro = null;
    if (typeof ResizeObserver !== "undefined") {
      ro = new ResizeObserver(() => applySize());
      ro.observe(container);
    }

    return () => {
      chart.unsubscribeCrosshairMove(onCrosshair);
      if (ro) ro.disconnect();
      chart.remove();
    };
  }, []);

  // Apply theme (chart + legend styles)
  useEffect(() => {
    const container = containerRef.current;
    const chart = container?.__chart;
    if (!chart) return;

    if (darkMode) {
      chart.applyOptions({
        layout: { background: { color: "#0f172a" }, textColor: "#e5e7eb" },
        grid: { vertLines: { color: "#243044" }, horzLines: { color: "#243044" } },
      });

      if (legendRef.current) {
        legendRef.current.style.background = "rgba(15, 23, 42, 0.75)";
        legendRef.current.style.color = "#e5e7eb";
        legendRef.current.style.borderColor = "#243044";
      }
    } else {
      chart.applyOptions({
        layout: { background: { color: "#ffffff" }, textColor: "#111827" },
        grid: { vertLines: { color: "#e5e7eb" }, horzLines: { color: "#e5e7eb" } },
      });

      if (legendRef.current) {
        legendRef.current.style.background = "rgba(255, 255, 255, 0.85)";
        legendRef.current.style.color = "#111827";
        legendRef.current.style.borderColor = "#e5e7eb";
      }
    }
  }, [darkMode]);

  // Update series data
  useEffect(() => {
    const container = containerRef.current;
    const chart = container?.__chart;
    const candleSeries = container?.__candleSeries;
    if (!chart || !candleSeries) return;

    candleSeries.setData(data || []);
    chart.timeScale().fitContent();

    if (legendRef.current) legendRef.current.textContent = "Hover chart to see details";
  }, [data]);

  // Update markers (P/V)
  useEffect(() => {
    const container = containerRef.current;
    const markersApi = container?.__markersApi;
    if (!markersApi) return;

    markersApi.setMarkers(swingMarkers);
  }, [swingMarkers]);

  return (
    <div className="chartBox" style={{ position: "relative" }}>
      <div
        ref={legendRef}
        style={{
          position: "absolute",
          top: 12,
          left: 12,
          padding: "8px 10px",
          borderRadius: 12,
          border: "1px solid",
          fontSize: 13,
          zIndex: 10,
          pointerEvents: "none",
          backdropFilter: "blur(6px)",
        }}
      >
        Hover chart to see details
      </div>

      <div
        ref={containerRef}
        style={{
          width: "100%",
          height: "100%",
          border: "1px solid var(--border)",
          borderRadius: 12,
          overflow: "hidden",
        }}
      />
    </div>
  );
}
