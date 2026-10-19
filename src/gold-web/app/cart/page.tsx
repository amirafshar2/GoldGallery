"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Trash2, Lock } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { readCart, writeCart, type CartItem } from "@/lib/cart";
import { toman, grams } from "@/lib/format";
import { useSite, useMe } from "@/components/site-provider";
import { Button } from "@/components/ui/button";

type Quote = { id: string; totalRial: number; expiresAt: string; lines: { pieceId: string; productName: string; totalRial: number; wageRial: number; taxRial: number; rawGoldRial: number; profitRial: number }[] };

export default function CartPage() {
  const { t } = useSite();
  const me = useMe();
  const router = useRouter();
  const [items, setItems] = useState<CartItem[]>([]);
  const [quote, setQuote] = useState<Quote | null>(null);
  const [left, setLeft] = useState(0);
  const [err, setErr] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => setItems(readCart()), []);
  useEffect(() => {
    if (!quote) return;
    const tick = () => setLeft(Math.max(0, Math.floor((new Date(quote.expiresAt).getTime() - Date.now()) / 1000)));
    tick(); const id = setInterval(tick, 1000); return () => clearInterval(id);
  }, [quote]);

  const remove = (id: string) => { const n = items.filter((i) => i.pieceId !== id); setItems(n); writeCart(n); setQuote(null); };

  const lock = async () => {
    if (!me) { router.push("/login?next=/cart"); return; }
    setErr(""); setBusy(true);
    try { setQuote(await api<Quote>("/quotes", { method: "POST", json: { pieceIds: items.map((i) => i.pieceId) } })); }
    catch (e) { setErr((e as ApiError).message); } finally { setBusy(false); }
  };

  const pay = async () => {
    if (!quote) return;
    setErr(""); setBusy(true);
    try {
      const order = await api<{ id: string }>("/orders", { method: "POST", json: { quoteId: quote.id } });
      const pay = await api<{ redirectUrl: string }>(`/orders/${order.id}/payments`, { method: "POST", headers: { "Idempotency-Key": crypto.randomUUID() } });
      writeCart([]);
      window.location.href = pay.redirectUrl;
    } catch (e) { setErr((e as ApiError).message); setQuote(null); } finally { setBusy(false); }
  };

  if (items.length === 0) return <div className="py-24 text-center text-muted-foreground">{t("cart.empty")} <Link href="/shop" className="text-primary">{t("nav.shop")}</Link></div>;

  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-10">
      <h1 className="text-3xl font-extrabold">{t("cart.title")}</h1>
      <div className="divide-y rounded-[var(--radius)] border bg-card">
        {items.map((i) => {
          const line = quote?.lines.find((l) => l.pieceId === i.pieceId);
          return (
            <div key={i.pieceId} className="flex items-center justify-between gap-3 p-4">
              <div><Link href={`/shop/${i.slug}`} className="font-semibold">{i.productName}</Link><p className="text-sm text-muted-foreground">{grams(i.grams)} گرم</p></div>
              <div className="flex items-center gap-3">
                <span className="num font-bold">{toman(line?.totalRial ?? i.priceRial)}</span>
                {!quote && <button onClick={() => remove(i.pieceId)} aria-label="حذف" className="text-muted-foreground hover:text-destructive"><Trash2 className="size-4" /></button>}
              </div>
            </div>
          );
        })}
      </div>
      {err && <p className="rounded-xl border border-destructive/50 p-3 text-sm text-destructive" data-testid="cart-error">{err}</p>}
      {!quote ? (
        <Button size="lg" className="w-full" onClick={lock} disabled={busy} data-testid="lock-price"><Lock /> {t("cart.lock")}</Button>
      ) : (
        <div className="space-y-4 rounded-[var(--radius)] border border-primary/50 bg-card p-5">
          <div className="flex items-center justify-between"><span>{t("cart.locked")}</span><b className="num text-xl text-primary" data-testid="quote-timer">{Math.floor(left / 60)}:{String(left % 60).padStart(2, "0")}</b></div>
          <div className="flex items-center justify-between text-lg"><span>جمع کل</span><b className="num" data-testid="quote-total">{toman(quote.totalRial)} تومان</b></div>
          <Button size="lg" className="w-full" onClick={pay} disabled={busy || left === 0} data-testid="pay">{left === 0 ? "قیمت منقضی شد — دوباره قفل کنید" : t("cart.pay")}</Button>
          {left === 0 && <Button variant="outline" className="w-full" onClick={lock}>محاسبه‌ی دوباره</Button>}
        </div>
      )}
    </div>
  );
}
