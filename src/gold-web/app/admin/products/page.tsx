"use client";

import { Fragment, useEffect, useState } from "react";
import { Plus, ChevronDown } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { euro, grams, pct } from "@/lib/format";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { ImageField, Toggle, Field } from "@/components/admin/fields";
import { RowActions, Notice } from "@/components/admin/crud";

type Card = { id: string; slug: string; name: string; categoryName: string; wageBp: number; startingFromRial: number | null; availablePieces: number };
type Prod = { slug: string; name: string; description: string; categoryId: string; audience: number; color: number; wageKind: number; wageValue: number; imageUrl: string; isFeatured: boolean };
type Piece = { id: string; productId: string; branchId: string; branch: string; barcode: string; goldGrams: number; karat: number; size: string; stoneGrams: number; stoneValueRial: number; status: string };
const EMPTY_P: Prod = { slug: "", name: "", description: "", categoryId: "", audience: 1, color: 1, wageKind: 1, wageValue: 1500, imageUrl: "", isFeatured: false };
const STATUS: Record<string, string> = { Available: "verfügbar", Reserved: "reserviert", Sold: "verkauft" };

export default function Products() {
  const [list, setList] = useState<Card[]>([]);
  const [cats, setCats] = useState<{ id: string; name: string }[]>([]);
  const [branches, setBranches] = useState<{ id: string; name: string }[]>([]);
  const [form, setForm] = useState<Prod | null>(null);
  const [editId, setEditId] = useState<string | null>(null);
  const [openId, setOpenId] = useState<string | null>(null);
  const [msg, setMsg] = useState({ text: "", ok: false });
  const load = () => api<{ items: Card[] }>("/products?pageSize=60&sort=newest").then((r) => setList(r.items));
  useEffect(() => { load(); api<{ id: string; name: string }[]>("/categories").then(setCats); api<{ id: string; name: string }[]>("/branches").then(setBranches); }, []);

  const startNew = () => { setEditId(null); setForm({ ...EMPTY_P, categoryId: cats[0]?.id ?? "" }); };
  const startEdit = async (id: string) => { setEditId(id); setForm(await api<Prod>(`/admin/products/${id}`)); };
  const save = async () => {
    if (!form) return;
    try {
      if (editId) await api(`/admin/products/${editId}`, { method: "PUT", json: form });
      else await api("/admin/products", { method: "POST", json: form });
      setForm(null); setMsg({ text: "Produkt gespeichert ✓", ok: true }); load(); window.dispatchEvent(new Event("site-saved"));
    } catch (e) { setMsg({ text: (e as ApiError).message, ok: false }); }
  };
  const del = async (id: string) => { try { await api(`/admin/products/${id}`, { method: "DELETE" }); load(); } catch (e) { setMsg({ text: (e as ApiError).message, ok: false }); } };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between"><h1 className="text-2xl font-extrabold">Produkte und Stücke</h1><Button onClick={startNew} disabled={!cats.length} data-testid="new-product"><Plus /> Neues Produkt</Button></div>
      {!cats.length && <Notice text="Bitte zuerst eine Kategorie anlegen." />}
      <Notice {...msg} />
      {form && (
        <div className="grid gap-4 rounded-[var(--radius)] border border-primary/50 bg-card p-5 sm:grid-cols-2" data-testid="product-form">
          <Field label="Produktname"><Input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} data-testid="p-name" /></Field>
          <Field label="URL-Name auf Englisch (für die Adresse)"><Input dir="ltr" value={form.slug} disabled={!!editId} onChange={(e) => setForm({ ...form, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, "-") })} data-testid="p-slug" /></Field>
          <Field label="Kategorie"><select className="h-9 w-full rounded-md border bg-background px-2" value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>{cats.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}</select></Field>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Zielgruppe"><select className="h-9 w-full rounded-md border bg-background px-2" value={form.audience} onChange={(e) => setForm({ ...form, audience: Number(e.target.value) })}><option value={1}>Damen</option><option value={2}>Herren</option><option value={3}>Kinder</option><option value={4}>Alle</option></select></Field>
            <Field label="Farbe"><select className="h-9 w-full rounded-md border bg-background px-2" value={form.color} onChange={(e) => setForm({ ...form, color: Number(e.target.value) })}><option value={1}>Gelbgold</option><option value={2}>Weißgold</option><option value={3}>Roségold</option></select></Field>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <Field label="Art des Arbeitslohns"><select className="h-9 w-full rounded-md border bg-background px-2" value={form.wageKind} onChange={(e) => setForm({ ...form, wageKind: Number(e.target.value), wageValue: Number(e.target.value) === 1 ? 1500 : 5000000 })}><option value={1}>prozentual</option><option value={2}>fest pro Gramm</option></select></Field>
            <Field label={form.wageKind === 1 ? "Arbeitslohn (%)" : "Arbeitslohn pro Gramm (€)"}>
              <Input dir="ltr" value={form.wageKind === 1 ? form.wageValue / 100 : form.wageValue / 100} onChange={(e) => setForm({ ...form, wageValue: Math.round(Number(e.target.value) * (form.wageKind === 1 ? 100 : 10)) })} data-testid="p-wage" />
            </Field>
          </div>
          <Toggle label="Hervorgehoben (oben in der Liste)" value={form.isFeatured} onChange={(x) => setForm({ ...form, isFeatured: x })} />
          <div className="sm:col-span-2"><Field label="Beschreibung"><Textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></Field></div>
          <ImageField label="Produktbild" value={form.imageUrl} onChange={(x) => setForm({ ...form, imageUrl: x })} />
          <div className="flex items-end gap-2"><Button onClick={save} data-testid="p-save">Speichern</Button><Button variant="ghost" onClick={() => setForm(null)}>Abbrechen</Button></div>
        </div>
      )}
      <div className="divide-y rounded-[var(--radius)] border bg-card">
        {list.map((p) => (
          <Fragment key={p.id}>
            <div className="flex flex-wrap items-center justify-between gap-2 p-3" data-testid="product-row">
              <button className="flex items-center gap-2 text-left" onClick={() => setOpenId(openId === p.id ? null : p.id)}>
                <ChevronDown className={`size-4 transition-transform ${openId === p.id ? "rotate-180" : ""}`} />
                <span><b>{p.name}</b> <span className="text-xs text-muted-foreground">{p.categoryName} · Arbeitslohn {pct(p.wageBp)} · {p.availablePieces} Stück · ab {euro(p.startingFromRial)}</span></span>
              </button>
              <RowActions onEdit={() => startEdit(p.id)} onDelete={() => del(p.id)} />
            </div>
            {openId === p.id && <Pieces productId={p.id} branches={branches} onChange={load} />}
          </Fragment>
        ))}
        {list.length === 0 && <p className="p-4 text-sm text-muted-foreground">Noch keine Produkte angelegt.</p>}
      </div>
    </div>
  );
}

function Pieces({ productId, branches, onChange }: { productId: string; branches: { id: string; name: string }[]; onChange: () => void }) {
  const empty = { productId, branchId: branches[0]?.id ?? "", barcode: "", goldGrams: "", karat: "750", size: "", stoneGrams: "0", stoneValueRial: "0" };
  const [rows, setRows] = useState<Piece[]>([]);
  const [f, setF] = useState(empty);
  const [edit, setEdit] = useState<string | null>(null);
  const [err, setErr] = useState("");
  const load = () => api<Piece[]>(`/admin/pieces?productId=${productId}`).then(setRows);
  useEffect(() => { load(); }, [productId]); // eslint-disable-line react-hooks/exhaustive-deps
  const body = () => ({ ...f, goldGrams: Number(f.goldGrams), karat: Number(f.karat), stoneGrams: Number(f.stoneGrams), stoneValueRial: Math.round(Number(String(f.stoneValueRial).replace(",", ".")) * 100) });
  const save = async () => {
    setErr("");
    try {
      if (edit) await api(`/admin/pieces/${edit}`, { method: "PUT", json: body() }); else await api("/admin/pieces", { method: "POST", json: body() });
      setF(empty); setEdit(null); load(); onChange(); window.dispatchEvent(new Event("site-saved"));
    } catch (e) { setErr((e as ApiError).message); }
  };
  const del = async (id: string) => { try { await api(`/admin/pieces/${id}`, { method: "DELETE" }); load(); onChange(); } catch (e) { setErr((e as ApiError).message); } };
  return (
    <div className="space-y-3 bg-muted/30 p-3" data-testid="pieces-admin">
      {!branches.length && <Notice text="Bitte zuerst eine Filiale anlegen." />}
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-7">
        <select className="h-9 rounded-md border bg-background px-2 text-sm" value={f.branchId} onChange={(e) => setF({ ...f, branchId: e.target.value })}>{branches.map((b) => <option key={b.id} value={b.id}>{b.name}</option>)}</select>
        <Input placeholder="Barcode" dir="ltr" value={f.barcode} disabled={!!edit} onChange={(e) => setF({ ...f, barcode: e.target.value })} data-testid="pc-barcode" />
        <Input placeholder="Gewicht (g) 1.440" dir="ltr" value={f.goldGrams} onChange={(e) => setF({ ...f, goldGrams: e.target.value })} data-testid="pc-grams" />
        <Input placeholder="Feingehalt 750" dir="ltr" value={f.karat} onChange={(e) => setF({ ...f, karat: e.target.value })} />
        <Input placeholder="Größe" value={f.size} onChange={(e) => setF({ ...f, size: e.target.value })} />
        <Input placeholder="Wert des Steins (€)" dir="ltr" value={f.stoneValueRial} onChange={(e) => setF({ ...f, stoneValueRial: e.target.value })} />
        <Button onClick={save} data-testid="pc-save">{edit ? "Bearbeiten" : "Stück hinzufügen"}</Button>
      </div>
      {err && <p className="text-sm text-destructive">{err}</p>}
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead><tr className="text-muted-foreground"><th className="p-2 text-left">Barcode</th><th className="p-2 text-left">Filiale</th><th className="p-2 text-left">Gewicht</th><th className="p-2 text-left">Feingehalt</th><th className="p-2 text-left">Größe</th><th className="p-2 text-left">Status</th><th /></tr></thead>
          <tbody>{rows.map((r) => (
            <tr key={r.id} className="border-t" data-testid="piece-row">
              <td className="p-2 num">{r.barcode}</td><td className="p-2">{r.branch}</td><td className="p-2 num">{grams(r.goldGrams)}</td><td className="p-2 num">{r.karat}</td><td className="p-2">{r.size}</td><td className="p-2">{STATUS[r.status]}</td>
              <td className="p-2">{r.status === "Available" && <RowActions onEdit={() => { setEdit(r.id); setF({ productId, branchId: r.branchId, barcode: r.barcode, goldGrams: String(r.goldGrams), karat: String(r.karat), size: r.size, stoneGrams: String(r.stoneGrams), stoneValueRial: String(r.stoneValueRial / 10) }); }} onDelete={() => del(r.id)} />}</td>
            </tr>))}</tbody>
        </table>
      </div>
    </div>
  );
}
