import { useEffect, useRef } from "react";
import { createChart, CrosshairMode, CandlestickSeries } from "lightweight-charts";

/**
 * data: array of { time: "YYYY-MM-DD", open, high, low, close }
 * height: number
 */
export default function CandlestickChart({ data, height = 420 }) {
  const containerRef = useRef(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const chart = createChart(containerRef.current, {
      width: containerRef.current.clientWidth,
      height,
      layout: { background: { color: "#ffffff" }, textColor: "#111827" },
      grid: { vertLines: { visible: true }, horzLines: { visible: true } },
      crosshair: { mode: CrosshairMode.Normal },
      rightPriceScale: { borderVisible: true },
      timeScale: { borderVisible: true, timeVisible: true },
    });

    // ✅ NEW API: addSeries(CandlestickSeries, options)
    const candleSeries = chart.addSeries(CandlestickSeries, {});

    containerRef.current.__chart = chart;
    containerRef.current.__candleSeries = candleSeries;

    chart.timeScale().fitContent();

    let ro = null;
    if (typeof ResizeObserver !== "undefined") {
      ro = new ResizeObserver(() => {
        chart.applyOptions({ width: containerRef.current.clientWidth });
      });
      ro.observe(containerRef.current);
    }

    return () => {
      if (ro) ro.disconnect();
      chart.remove();
    };
  }, [height]);

  useEffect(() => {
    const chart = containerRef.current?.__chart;
    const candleSeries = containerRef.current?.__candleSeries;
    if (!chart || !candleSeries) return;

    candleSeries.setData(data || []);
    chart.timeScale().fitContent();
  }, [data]);

  return (
    <div
      ref={containerRef}
      style={{
        width: "100%",
        border: "1px solid #e5e7eb",
        borderRadius: 12,
        overflow: "hidden",
      }}
    />
  );
}
