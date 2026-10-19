"use client";

import { useEffect, useState } from "react";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { toman } from "@/lib/format";
import { useSite } from "./site-provider";

type Rate = { sellPerGram18Rial: number; buyPerGram18Rial: number; asOf: string; canSellOnline: boolean };

export function useLiveRate() {
  const [rate, setRate] = useState<Rate | null>(null);
  useEffect(() => {
    let alive = true;
    fetch("/api/v1/rates/current").then((r) => (r.ok ? r.json() : null)).then((r) => alive && setRate(r)).catch(() => {});
    const conn = new HubConnectionBuilder().withUrl("/hubs/rates").withAutomaticReconnect().configureLogging(LogLevel.None).build();
    conn.on("RateChanged", (r: Rate) => setRate(r));
    conn.start().catch(() => {});
    return () => { alive = false; conn.stop(); };
  }, []);
  return rate;
}

export function RateTicker({ compact = false }: { compact?: boolean }) {
  const rate = useLiveRate();
  const { t } = useSite();
  if (!rate) return null;
  return (
    <div className={compact ? "flex items-center gap-2 text-xs" : "flex flex-wrap items-center justify-center gap-x-6 gap-y-1 text-sm"}>
      <span className="relative flex size-2">
        <span className={`absolute inline-flex size-full animate-ping rounded-full opacity-60 ${rate.canSellOnline ? "bg-emerald-400" : "bg-amber-400"}`} />
        <span className={`relative inline-flex size-2 rounded-full ${rate.canSellOnline ? "bg-emerald-400" : "bg-amber-400"}`} />
      </span>
      <span className="text-muted-foreground">{t("rate.label")}</span>
      <b className="num gold-text text-base">{toman(rate.sellPerGram18Rial)}</b>
      <span className="text-muted-foreground">{t("rate.toman")}</span>
      {!rate.canSellOnline && !compact && <span className="text-amber-400">{t("rate.paused")}</span>}
    </div>
  );
}
