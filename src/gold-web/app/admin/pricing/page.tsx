"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { toman, faDate } from "@/lib/format";
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
      await api("/admin/rates", { method: "POST", json: { sellPerGram18Rial: Number(sell) * 10, buyPerGram18Rial: Number(buy) * 10, confirmLargeJump: confirmJump } });
      setMsg("نرخ ثبت شد ✓ — همه‌ی قیمت‌ها فوراً به‌روز شدند."); setConfirmJump(false); load(); window.dispatchEvent(new Event("site-saved"));
    } catch (e) { const er = e as ApiError; setMsg(er.message); if (er.status === 409) setConfirmJump(true); }
  };
  const saveSettings = async () => {
    setMsg("");
    try { await api("/admin/pricing-settings", { method: "PUT", json: s }); setMsg("تنظیمات قیمت ذخیره شد ✓"); window.dispatchEvent(new Event("site-saved")); }
    catch (e) { setMsg((e as ApiError).message); }
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">نرخ طلا و اجرت</h1>
      {msg && <p className="rounded-xl border p-3 text-sm" data-testid="pricing-msg">{msg}</p>}
      <section className="space-y-4 rounded-[var(--radius)] border bg-card p-5">
        <h2 className="font-bold">نرخ امروز طلای ۱۸ عیار (هر گرم)</h2>
        {rate ? <p className="text-sm text-muted-foreground">نرخ فعلی: فروش <b className="num">{toman(rate.sellPerGram18Rial)}</b> · خرید <b className="num">{toman(rate.buyPerGram18Rial)}</b> تومان — {faDate(rate.asOf)} — وضعیت: {rate.health === "Ok" ? "سالم" : "کهنه/مشکوک (فروش آنلاین متوقف)"}</p>
          : <p className="text-sm text-amber-700">هنوز نرخی ثبت نشده — تا ثبت نرخ، فروش آنلاین فعال نمی‌شود.</p>}
        <div className="grid gap-4 sm:grid-cols-2">
          <TextField label="نرخ فروش (تومان)" ltr value={sell} onChange={setSell} placeholder="25895000" />
          <TextField label="نرخ خرید طلای کهنه (تومان)" ltr value={buy} onChange={setBuy} placeholder="25545000" />
        </div>
        {confirmJump && <label className="flex items-center gap-2 text-sm text-amber-700"><input type="checkbox" checked={confirmJump} onChange={(e) => setConfirmJump(e.target.checked)} /> تغییر بزرگ را تأیید می‌کنم (تأیید دوم)</label>}
        <Button onClick={saveRate} data-testid="save-rate">ثبت نرخ</Button>
      </section>
      {s && (
        <section className="space-y-4 rounded-[var(--radius)] border bg-card p-5">
          <h2 className="font-bold">سود، مالیات و گرد کردن</h2>
          <p className="text-xs text-muted-foreground">⚠ نرخ مالیات و سود را قبل از راه‌اندازی با حسابدار و اتحادیه تأیید کنید. اجرت هر محصول در صفحه‌ی همان محصول تنظیم می‌شود.</p>
          <div className="grid gap-4 sm:grid-cols-3">
            <TextField label="سود فروشنده (٪)" ltr value={String(s.profitBp / 100)} onChange={(x) => setS({ ...s, profitBp: Math.round(Number(x) * 100) })} />
            <TextField label="مالیات روی اجرت و سود (٪)" ltr value={String(s.taxBp / 100)} onChange={(x) => setS({ ...s, taxBp: Math.round(Number(x) * 100) })} />
            <TextField label="گرد کردن به (تومان)" ltr value={String(s.roundingIncrementRial / 10)} onChange={(x) => setS({ ...s, roundingIncrementRial: Math.max(1, Math.round(Number(x) * 10)) })} />
          </div>
          <select value={s.roundingDirection} onChange={(e) => setS({ ...s, roundingDirection: Number(e.target.value) })} className="h-9 rounded-md border bg-background px-3 text-sm">
            <option value={1}>رو به پایین (به نفع مشتری)</option><option value={2}>نزدیک‌ترین</option><option value={3}>رو به بالا</option>
          </select>
          <div><Button onClick={saveSettings}>ذخیره‌ی تنظیمات</Button></div>
        </section>
      )}
    </div>
  );
}
