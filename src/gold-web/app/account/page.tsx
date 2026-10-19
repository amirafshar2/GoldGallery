"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, logout } from "@/lib/api";
import { toman, faDate, grams, STATUS_FA, ROLE_FA } from "@/lib/format";
import { useMe } from "@/components/site-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";

type Order = { id: string; number: string; status: string; totalRial: number; createdAt: string; items: { productName: string; goldGrams: number; priceRial: number }[] };

export default function Account() {
  const me = useMe();
  const router = useRouter();
  const [orders, setOrders] = useState<Order[] | null>(null);
  useEffect(() => { api<Order[]>("/orders").then(setOrders).catch(() => router.push("/login?next=/account")); }, [me, router]);
  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-10">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-extrabold">حساب من</h1>
        <Button variant="outline" onClick={async () => { await logout(); router.push("/"); }}>خروج</Button>
      </div>
      {me && <p className="text-muted-foreground">{me.fullName || me.phone} · {ROLE_FA[me.role] ?? me.role}</p>}
      <h2 className="text-xl font-bold">سفارش‌های من</h2>
      {orders?.length === 0 && <p className="text-muted-foreground">هنوز سفارشی ندارید.</p>}
      <div className="grid gap-3">
        {orders?.map((o) => (
          <div key={o.id} className="rounded-[var(--radius)] border bg-card p-4" data-testid="my-order">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <b className="num">{o.number}</b><Badge variant="secondary">{STATUS_FA[o.status] ?? o.status}</Badge>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">{faDate(o.createdAt)}</p>
            <ul className="mt-3 space-y-1 text-sm">{o.items.map((i, k) => <li key={k}>{i.productName} — {grams(i.goldGrams)} گرم — <span className="num">{toman(i.priceRial)}</span></li>)}</ul>
            <p className="mt-2 text-left font-bold num">{toman(o.totalRial)} تومان</p>
          </div>
        ))}
      </div>
    </div>
  );
}
