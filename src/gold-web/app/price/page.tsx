"use client";

import { useEffect, useState } from "react";
import { RateTicker } from "@/components/rate-ticker";
import { toman, faDate } from "@/lib/format";

type Point = { asOf: string; sellPerGram18Rial: number; buyPerGram18Rial: number };

export default function PricePage() {
  const [pts, setPts] = useState<Point[]>([]);
  useEffect(() => { fetch("/api/v1/rates/history?days=30").then((r) => r.json()).then(setPts).catch(() => {}); }, []);
  const vals = pts.map((p) => p.sellPerGram18Rial);
  const min = Math.min(...vals), max = Math.max(...vals);
  const W = 800, H = 260, pad = 10;
  const x = (i: number) => pad + (i * (W - 2 * pad)) / Math.max(1, pts.length - 1);
  const y = (v: number) => H - pad - ((v - min) / Math.max(1, max - min)) * (H - 2 * pad);
  const d = pts.map((p, i) => `${i ? "L" : "M"}${x(i)},${y(p.sellPerGram18Rial)}`).join(" ");
  return (
    <div className="mx-auto max-w-5xl space-y-8 px-4 py-10">
      <h1 className="text-3xl font-extrabold">قیمت روز طلا</h1>
      <div className="rounded-[var(--radius)] border bg-card p-6"><RateTicker /></div>
      {pts.length > 1 && (
        <div className="rounded-[var(--radius)] border bg-card p-4">
          <p className="mb-3 text-sm text-muted-foreground">نمودار ۳۰ روز اخیر — طلای ۱۸ عیار (تومان/گرم)</p>
          <svg viewBox={`0 0 ${W} ${H}`} className="h-auto w-full" role="img" aria-label="نمودار قیمت">
            <defs><linearGradient id="g" x1="0" x2="0" y1="0" y2="1"><stop offset="0" stopColor="var(--brand-primary)" stopOpacity=".35" /><stop offset="1" stopColor="var(--brand-primary)" stopOpacity="0" /></linearGradient></defs>
            <path d={`${d} L${x(pts.length - 1)},${H} L${x(0)},${H} Z`} fill="url(#g)" />
            <path d={d} fill="none" stroke="var(--brand-primary)" strokeWidth="2.5" />
          </svg>
          <div className="mt-2 flex justify-between text-xs text-muted-foreground"><span>{faDate(pts[0].asOf)}</span><span>کمترین {toman(min)} · بیشترین {toman(max)}</span><span>{faDate(pts[pts.length - 1].asOf)}</span></div>
        </div>
      )}
    </div>
  );
}
