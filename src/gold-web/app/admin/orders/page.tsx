"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { toman, faDate, STATUS_FA } from "@/lib/format";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Notice } from "@/components/admin/crud";

type Order = { id: string; number: string; status: string; totalRial: number; createdAt: string; items: { productName: string; goldGrams: number }[] };
const NEXT: Record<string, [number, string][]> = {
  AwaitingPayment: [[9, "لغو"]], Paid: [[5, "بسته‌بندی شد"], [10, "مرجوع"]], Packed: [[6, "ارسال شد"], [7, "تحویل حضوری"]], Shipped: [[8, "تحویل شد"]], PickedUp: [[8, "تحویل شد"]],
};

export default function Orders() {
  const [rows, setRows] = useState<Order[]>([]);
  const [msg, setMsg] = useState("");
  const load = () => api<Order[]>("/admin/orders").then(setRows);
  useEffect(() => { load(); }, []);
  const move = async (id: string, status: number) => {
    setMsg("");
    try { await api(`/admin/orders/${id}/status`, { method: "PUT", json: { status } }); load(); } catch (e) { setMsg((e as ApiError).message); }
  };
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">سفارش‌ها</h1>
      <Notice text={msg} />
      <div className="grid gap-3">
        {rows.map((o) => (
          <div key={o.id} className="flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius)] border bg-card p-4" data-testid="admin-order">
            <div>
              <p className="flex items-center gap-2"><b className="num">{o.number}</b><Badge variant="secondary">{STATUS_FA[o.status] ?? o.status}</Badge></p>
              <p className="text-xs text-muted-foreground">{faDate(o.createdAt)} · {o.items.map((i) => i.productName).join("، ")}</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <b className="num">{toman(o.totalRial)}</b>
              {(NEXT[o.status] ?? []).map(([s, l]) => <Button key={s} size="sm" variant="outline" onClick={() => move(o.id, s)}>{l}</Button>)}
            </div>
          </div>
        ))}
        {rows.length === 0 && <p className="text-sm text-muted-foreground">سفارشی ثبت نشده است.</p>}
      </div>
    </div>
  );
}
