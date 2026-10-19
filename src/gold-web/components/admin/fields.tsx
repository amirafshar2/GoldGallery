"use client";

import { useRef, useState } from "react";
import { ImagePlus, Trash2, ArrowUp, ArrowDown, Plus } from "lucide-react";
import { upload, ApiError } from "@/lib/api";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";

export function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return <div className="space-y-1.5"><Label>{label}</Label>{children}{hint && <p className="text-xs text-muted-foreground">{hint}</p>}</div>;
}

export function TextField({ label, hint, value, onChange, multiline, ltr, placeholder }: { label: string; hint?: string; value: string; onChange: (v: string) => void; multiline?: boolean; ltr?: boolean; placeholder?: string }) {
  return (
    <Field label={label} hint={hint}>
      {multiline ? <Textarea value={value ?? ""} placeholder={placeholder} onChange={(e) => onChange(e.target.value)} rows={4} />
        : <Input value={value ?? ""} placeholder={placeholder} dir={ltr ? "ltr" : undefined} onChange={(e) => onChange(e.target.value)} />}
    </Field>
  );
}

export function ColorField({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) {
  return (
    <Field label={label}>
      <div className="flex items-center gap-2">
        <input type="color" value={/^#[0-9a-fA-F]{6}$/.test(value) ? value : "#000000"} onChange={(e) => onChange(e.target.value.toUpperCase())} className="h-9 w-12 cursor-pointer rounded border bg-transparent" />
        <Input dir="ltr" value={value} onChange={(e) => onChange(e.target.value)} className="font-mono" />
      </div>
    </Field>
  );
}

export function Toggle({ label, value, onChange }: { label: string; value: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex cursor-pointer items-center justify-between gap-3 rounded-xl border p-3">
      <span className="text-sm">{label}</span>
      <button type="button" role="switch" aria-checked={value} onClick={() => onChange(!value)}
        className={`relative h-6 w-11 rounded-full transition-colors ${value ? "bg-primary" : "bg-muted"}`}>
        <span className={`absolute top-0.5 size-5 rounded-full bg-white transition-all ${value ? "left-0.5" : "left-[22px]"}`} />
      </button>
    </label>
  );
}

export function ImageField({ label, hint, value, onChange, video = false }: { label: string; hint?: string; value: string; onChange: (v: string) => void; video?: boolean }) {
  const ref = useRef<HTMLInputElement>(null);
  const [err, setErr] = useState("");
  const [busy, setBusy] = useState(false);
  return (
    <Field label={label} hint={hint}>
      <div className="flex items-center gap-3">
        <div className="grid size-20 place-items-center overflow-hidden rounded-xl border bg-muted">
          {value ? (video ? <video src={value} muted className="size-full object-cover" /> : <img src={value} alt="" className="size-full object-contain" />) : <ImagePlus className="size-6 text-muted-foreground" />}
        </div>
        <div className="space-y-2">
          <input ref={ref} type="file" accept={video ? "video/mp4" : "image/png,image/jpeg,image/webp,image/avif"} hidden onChange={async (e) => {
            const f = e.target.files?.[0]; if (!f) return;
            setErr(""); setBusy(true);
            try { onChange(await upload(f)); } catch (x) { setErr((x as ApiError).message); } finally { setBusy(false); e.target.value = ""; }
          }} />
          <div className="flex gap-2">
            <Button type="button" size="sm" variant="outline" disabled={busy} onClick={() => ref.current?.click()}>{busy ? "در حال آپلود…" : video ? "انتخاب ویدیو (MP4)" : "انتخاب تصویر"}</Button>
            {value && <Button type="button" size="sm" variant="ghost" onClick={() => onChange("")}><Trash2 /></Button>}
          </div>
          {err && <p className="text-xs text-destructive">{err}</p>}
        </div>
      </div>
    </Field>
  );
}

export function ImageList({ label, hint, value, onChange, max = 7 }: { label: string; hint?: string; value: string[]; onChange: (v: string[]) => void; max?: number }) {
  const ref = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState("");
  return (
    <Field label={label} hint={hint}>
      <div className="grid grid-cols-3 gap-2 sm:grid-cols-4">
        {value.map((src, i) => (
          <div key={src + i} className="group relative aspect-square overflow-hidden rounded-xl border">
            <img src={src} alt="" className="size-full object-cover" />
            <div className="absolute inset-x-0 bottom-0 flex justify-between bg-black/60 p-1">
              <button type="button" onClick={() => i > 0 && onChange(swap(value, i, i - 1))} aria-label="قبلی"><ArrowUp className="size-4" /></button>
              <button type="button" onClick={() => onChange(value.filter((_, k) => k !== i))} aria-label="حذف"><Trash2 className="size-4 text-red-600" /></button>
              <button type="button" onClick={() => i < value.length - 1 && onChange(swap(value, i, i + 1))} aria-label="بعدی"><ArrowDown className="size-4" /></button>
            </div>
          </div>
        ))}
        {value.length < max && (
          <button type="button" disabled={busy} onClick={() => ref.current?.click()} className="grid aspect-square place-items-center rounded-xl border border-dashed text-muted-foreground hover:border-primary">
            {busy ? "…" : <Plus />}
          </button>
        )}
      </div>
      <input ref={ref} type="file" multiple accept="image/png,image/jpeg,image/webp,image/avif" hidden onChange={async (e) => {
        const files = Array.from(e.target.files ?? []).slice(0, max - value.length); if (!files.length) return;
        setBusy(true); setErr("");
        try { const urls: string[] = []; for (const f of files) urls.push(await upload(f)); onChange([...value, ...urls]); }
        catch (x) { setErr((x as ApiError).message); } finally { setBusy(false); e.target.value = ""; }
      }} />
      {err && <p className="text-xs text-destructive">{err}</p>}
    </Field>
  );
}

export function swap<T>(a: T[], i: number, j: number) { const c = [...a]; [c[i], c[j]] = [c[j], c[i]]; return c; }
