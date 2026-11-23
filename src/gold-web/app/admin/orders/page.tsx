"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { euro, date, STATUS_LABEL } from "@/lib/format";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Notice } from "@/components/admin/crud";

type Order = { id: string; number: string; status: string; totalRial: number; createdAt: string; items: { productName: string; goldGrams: number }[] };
const NEXT: Record<string, [number, string][]> = {
  AwaitingPayment: [[9, "Stornieren"]], Paid: [[5, "Verpackt"], [10, "Zurückerstattet"]], Packed: [[6, "Versendet"], [7, "Im Geschäft abgeholt"]], Shipped: [[8, "Zugestellt"]], PickedUp: [[8, "Zugestellt"]],
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
      <h1 className="text-2xl font-extrabold">Bestellungen</h1>
      <Notice text={msg} />
      <div className="grid gap-3">
        {rows.map((o) => (
          <div key={o.id} className="flex flex-wrap items-center justify-between gap-3 rounded-[var(--radius)] border bg-card p-4" data-testid="admin-order">
            <div>
              <p className="flex items-center gap-2"><b className="num">{o.number}</b><Badge variant="secondary">{STATUS_LABEL[o.status] ?? o.status}</Badge></p>
              <p className="text-xs text-muted-foreground">{date(o.createdAt)} · {o.items.map((i) => i.productName).join(",  ")}</p>
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <b className="num">{euro(o.totalRial)}</b>
              {(NEXT[o.status] ?? []).map(([s, l]) => <Button key={s} size="sm" variant="outline" onClick={() => move(o.id, s)}>{l}</Button>)}
            </div>
          </div>
        ))}
        {rows.length === 0 && <p className="text-sm text-muted-foreground">Noch keine Bestellungen.</p>}
      </div>
    </div>
  );
}
