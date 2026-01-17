import { useEffect, useRef } from "react";
import {
  createChart,
  CrosshairMode,
  CandlestickSeries,
} from "lightweight-charts";

function formatNumber(n) {
  if (n === null || n === undefined) return "-";
  const x = Number(n);
  return Number.isFinite(x) ? x.toFixed(2) : "-";
}

export default function CandlestickChart({ data, darkMode }) {
  const containerRef = useRef(null);
  const legendRef = useRef(null);

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

    container.__chart = chart;
    container.__candleSeries = candleSeries;

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

      // param.time is "YYYY-MM-DD" when you provide string time
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

  // Update data when it changes
  useEffect(() => {
    const container = containerRef.current;
    const chart = container?.__chart;
    const candleSeries = container?.__candleSeries;
    if (!chart || !candleSeries) return;

    candleSeries.setData(data || []);
    chart.timeScale().fitContent();

    // Reset legend
    if (legendRef.current) {
      legendRef.current.textContent = "Hover chart to see details";
    }
  }, [data]);

  // Apply theme (light/dark) when toggled
  useEffect(() => {
    const container = containerRef.current;
    const chart = container?.__chart;
    if (!chart) return;

    if (darkMode) {
      chart.applyOptions({
        layout: { background: { color: "#0f172a" }, textColor: "#e5e7eb" },
        grid: {
          vertLines: { color: "#243044" },
          horzLines: { color: "#243044" },
        },
      });
      if (legendRef.current) {
        legendRef.current.style.background = "rgba(15, 23, 42, 0.75)";
        legendRef.current.style.color = "#e5e7eb";
        legendRef.current.style.borderColor = "#243044";
      }
    } else {
      chart.applyOptions({
        layout: { background: { color: "#ffffff" }, textColor: "#111827" },
        grid: {
          vertLines: { color: "#e5e7eb" },
          horzLines: { color: "#e5e7eb" },
        },
      });
      if (legendRef.current) {
        legendRef.current.style.background = "rgba(255, 255, 255, 0.85)";
        legendRef.current.style.color = "#111827";
        legendRef.current.style.borderColor = "#e5e7eb";
      }
    }
  }, [darkMode]);

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
