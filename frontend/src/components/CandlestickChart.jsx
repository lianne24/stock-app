import { useEffect, useRef } from "react";
import { createChart, CrosshairMode, CandlestickSeries } from "lightweight-charts";

export default function CandlestickChart({ data }) {
  const containerRef = useRef(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const chart = createChart(containerRef.current, {
      width: containerRef.current.clientWidth,
      height: containerRef.current.clientHeight,
      layout: { background: { color: "#ffffff" }, textColor: "#111827" },
      grid: { vertLines: { visible: true }, horzLines: { visible: true } },
      crosshair: { mode: CrosshairMode.Normal },
      rightPriceScale: { borderVisible: true },
      timeScale: { borderVisible: true, timeVisible: true },
    });

    const candleSeries = chart.addSeries(CandlestickSeries, {});
    containerRef.current.__chart = chart;
    containerRef.current.__candleSeries = candleSeries;

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
      ro.observe(containerRef.current);
    }

    return () => {
      if (ro) ro.disconnect();
      chart.remove();
    };
  }, []);

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
        height: "100%",
        border: "1px solid #e5e7eb",
        borderRadius: 12,
        overflow: "hidden",
      }}
    />
  );
}
