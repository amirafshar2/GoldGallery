"use client";

import { useState } from "react";
import { api, ApiError } from "@/lib/api";
import { toman } from "@/lib/format";
import { useSite } from "@/components/site-provider";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function SellOldGold() {
  const { t, site } = useSite();
  const [unit, setUnit] = useState<"grams" | "mesghal">("grams");
  const [w, setW] = useState("");
  const [k, setK] = useState("750");
  const [r, setR] = useState<{ valueRial: number; grams: number; mesghal: number } | null>(null);
  const [err, setErr] = useState("");
  const calc = async () => {
    setErr("");
    try { setR(await api("/buyback/estimate", { method: "POST", json: { [unit]: Number(w), karat: Number(k) } })); }
    catch (e) { setErr((e as ApiError).message); setR(null); }
  };
  return (
    <div className="mx-auto max-w-xl space-y-6 px-4 py-10">
      <h1 className="text-3xl font-extrabold">{t("buyback.title")}</h1>
      <p className="text-muted-foreground">{t("buyback.hint")}</p>
      <div className="space-y-4 rounded-[var(--radius)] border bg-card p-6">
        <div className="flex gap-2">
          {(["grams", "mesghal"] as const).map((u) => <button key={u} onClick={() => setUnit(u)} className={`flex-1 rounded-full border py-2 text-sm ${unit === u ? "border-primary text-primary" : ""}`}>{u === "grams" ? "گرم" : "مثقال"}</button>)}
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-2"><Label>وزن</Label><Input inputMode="decimal" dir="ltr" value={w} onChange={(e) => setW(e.target.value)} data-testid="bb-weight" /></div>
          <div className="space-y-2"><Label>عیار (در هزار)</Label><Input inputMode="numeric" dir="ltr" value={k} onChange={(e) => setK(e.target.value)} /></div>
        </div>
        <Button className="w-full" onClick={calc} data-testid="bb-calc">محاسبه</Button>
        {err && <p className="text-sm text-destructive">{err}</p>}
        {r && <div className="rounded-xl bg-muted p-4 text-center" data-testid="bb-result"><p className="text-sm text-muted-foreground">ارزش تقریبی</p><p className="num text-3xl font-extrabold gold-text">{toman(r.valueRial)}</p><p className="text-sm">تومان</p></div>}
      </div>
      {site.contact.phone && <p className="text-center text-sm text-muted-foreground">برای نوبت ارزیابی حضوری: <a className="num text-primary" href={`tel:${site.contact.phone}`}>{site.contact.phone}</a></p>}
    </div>
  );
}
