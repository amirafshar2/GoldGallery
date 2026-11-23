"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { CheckCircle2, Circle } from "lucide-react";
import { api } from "@/lib/api";
import { euro, num } from "@/lib/format";
import { useMe } from "@/components/site-provider";
import { useOnboarding } from "@/components/admin/onboarding";

export default function Dashboard() {
  const me = useMe();
  const { ob } = useOnboarding();
  const [orders, setOrders] = useState<{ status: string; totalRial: number }[]>([]);
  const [ledger, setLedger] = useState<{ balanceRial: number; entries: number } | null>(null);
  useEffect(() => {
    api<{ status: string; totalRial: number }[]>("/admin/orders").then(setOrders).catch(() => {});
    if (me?.role === "Admin") api<{ balanceRial: number; entries: number }>("/admin/ledger/summary").then(setLedger).catch(() => {});
  }, [me]);
  const paid = orders.filter((o) => ["Paid", "Packed", "Shipped", "PickedUp", "Delivered"].includes(o.status));
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">Hallo {me?.fullName || ""} 👋</h1>
      <div className="grid gap-3 sm:grid-cols-3">
        <Stat label="Bestellungen" value={num(orders.length)} />
        <Stat label="Bezahlt" value={num(paid.length)} />
        {ledger && <Stat label="Kontostand (€)" value={euro(ledger.balanceRial)} />}
      </div>
      {ob && me?.role === "Admin" && (
        <section className="rounded-[var(--radius)] border bg-card p-5" data-testid="dashboard-checklist">
          <div className="mb-3 flex items-center justify-between"><h2 className="font-bold">Website einrichten</h2><b className="num text-primary">{num(ob.percent)} %</b></div>
          <div className="grid gap-2 sm:grid-cols-3">
            {ob.steps.map((s) => (
              <Link key={s.key} href={s.adminLink} className="flex items-start gap-2 rounded-xl border p-3 hover:border-primary">
                {s.done ? <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-emerald-700" /> : <Circle className="mt-0.5 size-4 shrink-0 text-muted-foreground" />}
                <span><b className="block text-sm">{s.title}</b><span className="text-xs text-muted-foreground">{s.description}</span></span>
              </Link>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return <div className="rounded-[var(--radius)] border bg-card p-5"><p className="text-sm text-muted-foreground">{label}</p><p className="num mt-1 text-2xl font-extrabold">{value}</p></div>;
}
