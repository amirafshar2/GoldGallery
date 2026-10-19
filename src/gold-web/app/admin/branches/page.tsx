"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Notice } from "@/components/admin/crud";

type Branch = { id: string; name: string; address: string };

export default function Branches() {
  const [rows, setRows] = useState<Branch[]>([]);
  const [f, setF] = useState({ name: "", address: "" });
  const [msg, setMsg] = useState({ text: "", ok: false });
  const load = () => api<Branch[]>("/branches").then(setRows);
  useEffect(() => { load(); }, []);
  const save = async () => {
    try { await api("/admin/branches", { method: "POST", json: f }); setF({ name: "", address: "" }); setMsg({ text: "شعبه اضافه شد ✓", ok: true }); load(); window.dispatchEvent(new Event("site-saved")); }
    catch (e) { setMsg({ text: (e as ApiError).message, ok: false }); }
  };
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">شعبه‌ها</h1>
      <Notice {...msg} />
      <div className="grid gap-3 rounded-[var(--radius)] border bg-card p-4 sm:grid-cols-[1fr_2fr_auto]">
        <Input placeholder="نام شعبه" value={f.name} onChange={(e) => setF({ ...f, name: e.target.value })} data-testid="branch-name" />
        <Input placeholder="آدرس" value={f.address} onChange={(e) => setF({ ...f, address: e.target.value })} />
        <Button onClick={save} data-testid="branch-save">افزودن</Button>
      </div>
      <div className="divide-y rounded-[var(--radius)] border bg-card">
        {rows.map((r) => <div key={r.id} className="p-3"><b>{r.name}</b> <span className="text-sm text-muted-foreground">{r.address}</span></div>)}
        {rows.length === 0 && <p className="p-4 text-sm text-muted-foreground">هنوز شعبه‌ای ندارید.</p>}
      </div>
    </div>
  );
}
