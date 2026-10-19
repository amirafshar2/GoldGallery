"use client";

import { useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { RowActions, Notice } from "@/components/admin/crud";

type Cat = { id: string; slug: string; name: string; sortOrder: number };
const EMPTY = { slug: "", name: "", sortOrder: 0 };

export default function Categories() {
  const [rows, setRows] = useState<Cat[]>([]);
  const [f, setF] = useState(EMPTY);
  const [edit, setEdit] = useState<string | null>(null);
  const [msg, setMsg] = useState({ text: "", ok: false });
  const load = () => api<Cat[]>("/categories").then(setRows);
  useEffect(() => { load(); }, []);

  const save = async () => {
    try {
      if (edit) await api(`/admin/categories/${edit}`, { method: "PUT", json: f });
      else await api("/admin/categories", { method: "POST", json: f });
      setF(EMPTY); setEdit(null); setMsg({ text: "ذخیره شد ✓", ok: true }); load(); window.dispatchEvent(new Event("site-saved"));
    } catch (e) { setMsg({ text: (e as ApiError).message, ok: false }); }
  };
  const del = async (id: string) => {
    try { await api(`/admin/categories/${id}`, { method: "DELETE" }); load(); } catch (e) { setMsg({ text: (e as ApiError).message, ok: false }); }
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-extrabold">دسته‌بندی‌ها</h1>
      <Notice {...msg} />
      <div className="grid gap-3 rounded-[var(--radius)] border bg-card p-4 sm:grid-cols-[1fr_1fr_100px_auto]">
        <Input placeholder="نام (مثلاً انگشتر)" value={f.name} onChange={(e) => setF({ ...f, name: e.target.value })} data-testid="cat-name" />
        <Input placeholder="نامک انگلیسی (rings)" dir="ltr" value={f.slug} disabled={!!edit} onChange={(e) => setF({ ...f, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, "-") })} data-testid="cat-slug" />
        <Input placeholder="ترتیب" type="number" value={f.sortOrder} onChange={(e) => setF({ ...f, sortOrder: Number(e.target.value) })} />
        <div className="flex gap-2"><Button onClick={save} data-testid="cat-save">{edit ? "ویرایش" : "افزودن"}</Button>{edit && <Button variant="ghost" onClick={() => { setEdit(null); setF(EMPTY); }}>انصراف</Button>}</div>
      </div>
      <div className="divide-y rounded-[var(--radius)] border bg-card">
        {rows.map((r) => (
          <div key={r.id} className="flex items-center justify-between gap-3 p-3" data-testid="cat-row">
            <span>{r.name} <code className="text-xs text-muted-foreground" dir="ltr">{r.slug}</code></span>
            <RowActions onEdit={() => { setEdit(r.id); setF({ slug: r.slug, name: r.name, sortOrder: r.sortOrder }); }} onDelete={() => del(r.id)} />
          </div>
        ))}
        {rows.length === 0 && <p className="p-4 text-sm text-muted-foreground">هنوز دسته‌ای ندارید.</p>}
      </div>
    </div>
  );
}
