"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Suspense } from "react";
import { api } from "@/lib/api";
import { useSite } from "@/components/site-provider";
import { ProductCardView, type ProductCard } from "@/components/product-card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

const AUDIENCES = [["", "همه"], ["1", "زنانه"], ["2", "مردانه"], ["3", "کودکانه"]];
const COLORS = [["", "همه"], ["1", "زرد"], ["2", "سفید"], ["3", "رزگلد"]];
const SORTS = [["popular", "محبوب‌ترین"], ["newest", "جدیدترین"], ["cheapest", "ارزان‌ترین"], ["expensive", "گران‌ترین"], ["low-wage", "کم‌اجرت‌ترین"], ["lightest", "سبک‌ترین"]];

function ShopInner() {
  const { t } = useSite();
  const sp = useSearchParams();
  const router = useRouter();
  const [cats, setCats] = useState<{ slug: string; name: string }[]>([]);
  const [data, setData] = useState<{ items: ProductCard[]; total: number } | null>(null);
  const q = Object.fromEntries(sp.entries());

  useEffect(() => { api<{ slug: string; name: string }[]>("/categories").then(setCats).catch(() => {}); }, []);
  useEffect(() => {
    setData(null);
    api<{ items: ProductCard[]; total: number }>(`/products?pageSize=60&${sp.toString()}`).then(setData).catch(() => setData({ items: [], total: 0 }));
  }, [sp]);

  const set = (k: string, v: string) => {
    const n = new URLSearchParams(sp.toString());
    if (v) n.set(k, v); else n.delete(k);
    router.replace(`/shop?${n.toString()}`, { scroll: false });
  };
  const Chips = ({ k, opts }: { k: string; opts: string[][] }) => (
    <div className="flex flex-wrap gap-2">
      {opts.map(([v, l]) => (
        <button key={v} onClick={() => set(k, v)} className={`rounded-full border px-3 py-1.5 text-sm ${(q[k] ?? "") === v ? "border-primary bg-primary text-primary-foreground" : "hover:border-primary"}`}>{l}</button>
      ))}
    </div>
  );

  return (
    <div className="mx-auto max-w-7xl px-4 py-10">
      <h1 className="mb-8 text-4xl font-light sm:text-5xl">{t("shop.title")}</h1>
      <div className="mb-8 grid gap-4 rounded-[var(--radius)] border bg-card p-4">
        <div className="flex flex-wrap gap-2">
          <button onClick={() => set("category", "")} className={`rounded-full border px-3 py-1.5 text-sm ${!q.category ? "border-primary text-primary" : ""}`}>همه‌ی دسته‌ها</button>
          {cats.map((c) => <button key={c.slug} onClick={() => set("category", c.slug)} className={`rounded-full border px-3 py-1.5 text-sm ${q.category === c.slug ? "border-primary text-primary" : ""}`}>{c.name}</button>)}
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          <Chips k="audience" opts={AUDIENCES} />
          <Chips k="color" opts={COLORS} />
          <select value={q.sort ?? "popular"} onChange={(e) => set("sort", e.target.value)} className="h-9 rounded-md border bg-background px-3 text-sm">
            {SORTS.map(([v, l]) => <option key={v} value={v}>{l}</option>)}
          </select>
        </div>
        <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
          <Input placeholder="جستجو…" defaultValue={q.search ?? ""} onBlur={(e) => set("search", e.target.value)} />
          <Input placeholder="حداقل وزن (گرم)" inputMode="decimal" defaultValue={q.minGrams ?? ""} onBlur={(e) => set("minGrams", e.target.value)} />
          <Input placeholder="حداکثر وزن (گرم)" inputMode="decimal" defaultValue={q.maxGrams ?? ""} onBlur={(e) => set("maxGrams", e.target.value)} />
          <Input placeholder="بودجه تا (تومان)" inputMode="numeric" defaultValue={q.maxPriceRial ? String(Number(q.maxPriceRial) / 10) : ""} onBlur={(e) => set("maxPriceRial", e.target.value ? String(Number(e.target.value) * 10) : "")} />
        </div>
      </div>
      {!data ? (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">{Array.from({ length: 8 }).map((_, i) => <Skeleton key={i} className="aspect-[3/4]" />)}</div>
      ) : data.items.length === 0 ? (
        <p className="py-20 text-center text-muted-foreground">{t("shop.empty")}</p>
      ) : (
        <div className="grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-5 lg:grid-cols-4">{data.items.map((p, i) => <ProductCardView key={p.id} p={p} index={i} />)}</div>
      )}
    </div>
  );
}

export default function ShopPage() {
  return <Suspense><ShopInner /></Suspense>;
}
