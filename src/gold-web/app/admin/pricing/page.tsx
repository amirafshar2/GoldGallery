"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { euro, date } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { TextField } from "@/components/admin/fields";

type Rate = { sellPerGram18Rial: number; buyPerGram18Rial: number; asOf: string; health: string };
type Settings = { profitBp: number; taxBp: number; roundingIncrementRial: number; roundingDirection: number };

export default function Pricing() {
  const [rate, setRate] = useState<Rate | null>(null);
  const [sell, setSell] = useState(""); const [buy, setBuy] = useState("");
  const [s, setS] = useState<Settings | null>(null);
  const [msg, setMsg] = useState(""); const [confirmJump, setConfirmJump] = useState(false);
  const load = () => { api<Rate>("/rates/current").then(setRate).catch(() => setRate(null)); api<Settings>("/admin/pricing-settings").then(setS).catch(() => {}); };
  useEffect(load, []);

  const saveRate = async () => {
    setMsg("");
    try {
      await api("/admin/rates", { method: "POST", json: { sellPerGram18Rial: Math.round(Number(sell.replace(",", ".")) * 100), buyPerGram18Rial: Math.round(Number(buy.replace(",", ".")) * 100), confirmLargeJump: confirmJump } });
      setMsg("Kurs gespeichert ✓ – alle Preise wurden sofort aktualisiert."); setConfirmJump(false); load(); window.dispatchEvent(new Event("site-saved"));
    } catch (e) { const er = e as ApiError; setMsg(er.message); if (er.status === 409) setConfirmJump(true); }
  };
  const saveSettings = async () => {
    setMsg("");
    try { await api("/admin/pricing-settings", { method: "PUT", json: s }); setMsg("Preiseinstellungen gespeichert ✓"); window.dispatchEvent(new Event("site-saved")); }
    catch (e) { setMsg((e as ApiError).message); }
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">Goldkurs und Arbeitslohn</h1>
      {msg && <p className="rounded-xl border p-3 text-sm" data-testid="pricing-msg">{msg}</p>}
      <section className="space-y-4 rounded-[var(--radius)] border bg-card p-5">
        <h2 className="font-bold">Heutiger Kurs für 750er Gold (pro Gramm)</h2>
        {rate ? <p className="text-sm text-muted-foreground">Aktueller Kurs: Verkauf <b className="num">{euro(rate.sellPerGram18Rial)}</b> · Ankauf <b className="num">{euro(rate.buyPerGram18Rial)}</b> € – {date(rate.asOf)} – Status: {rate.health === "Ok" ? "in Ordnung" : "veraltet/verdächtig (Onlineverkauf pausiert)"}</p>
          : <p className="text-sm text-amber-700">Noch kein Kurs erfasst – der Onlineverkauf startet erst, wenn ein Kurs gespeichert ist.</p>}
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="Verkaufskurs (€)" ltr value={sell} onChange={setSell} placeholder="25895000" />
          <TextField label="Ankaufskurs für Altgold (€)" ltr value={buy} onChange={setBuy} placeholder="25545000" />
        </div>
        {confirmJump && <label className="flex items-center gap-2 text-sm text-amber-700"><input type="checkbox" checked={confirmJump} onChange={(e) => setConfirmJump(e.target.checked)} /> Ich bestätige die große Änderung (zweite Bestätigung)</label>}
        <Button onClick={saveRate} data-testid="save-rate">Kurs speichern</Button>
      </section>
      {s && (
        <section className="space-y-4 rounded-[var(--radius)] border bg-card p-5">
          <h2 className="font-bold">Gewinn, Steuer und Rundung</h2>
          <p className="text-xs text-muted-foreground">⚠ Steuersatz und Gewinnaufschlag vor dem Start mit der Steuerberatung abstimmen. Den Arbeitslohn legen Sie beim jeweiligen Produkt fest.</p>
          <div className="grid gap-4 sm:grid-cols-3">
            <TextField label="Gewinnaufschlag (%)" ltr value={String(s.profitBp / 100)} onChange={(x) => setS({ ...s, profitBp: Math.round(Number(x) * 100) })} />
            <TextField label="Steuer auf Arbeitslohn und Gewinn (%)" ltr value={String(s.taxBp / 100)} onChange={(x) => setS({ ...s, taxBp: Math.round(Number(x) * 100) })} />
            <TextField label="Runden auf (€)" ltr value={String(s.roundingIncrementRial / 100)} onChange={(x) => setS({ ...s, roundingIncrementRial: Math.max(1, Math.round(Number(x.replace(",", ".")) * 100)) })} />
          </div>
          <select value={s.roundingDirection} onChange={(e) => setS({ ...s, roundingDirection: Number(e.target.value) })} className="h-9 rounded-md border bg-background px-3 text-sm">
            <option value={1}>abrunden (zugunsten des Kunden)</option><option value={2}>kaufmännisch</option><option value={3}>aufrunden</option>
          </select>
          <div><Button onClick={saveSettings}>Einstellungen speichern</Button></div>
        </section>
      )}
    </div>
  );
}
