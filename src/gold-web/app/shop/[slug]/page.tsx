"use client";

import { Fragment, use, useEffect, useState } from "react";
import { Gem, Check } from "lucide-react";
import { api } from "@/lib/api";
import { toman, grams, pct } from "@/lib/format";
import { addToCart } from "@/lib/cart";
import { useSite } from "@/components/site-provider";
import { useLiveRate } from "@/components/rate-ticker";
import { Button } from "@/components/ui/button";
import { Lens } from "@/components/ui21/lens";
import { BlurFade } from "@/components/ui21/blur-fade";
import Link from "next/link";
import { MessageCircle, CalendarCheck } from "lucide-react";
import { waLink } from "@/lib/utils";

/** Seeded demo photos have a 4K original next to the web size; uploaded photos are used as-is. */
const hiRes = (url: string) => { const m = url.match(/^\/img\/p\/([\w-]+)\.jpg$/); return m ? `/img/4k/${m[1]}.jpg` : url; };
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

type Piece = { pieceId: string; branch: string; goldGrams: number; karat: number; size: string; status: string; rawGoldRial: number; wageRial: number; profitRial: number; taxRial: number; stoneValueRial: number; totalRial: number };
type Detail = { name: string; slug: string; description: string; categoryName: string; imageUrl: string; wageKind: number; wageValue: number; canSellOnline: boolean; rateSellPerGram18Rial: number; pieces: Piece[] };

export default function ProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const { t, site } = useSite();
  const rate = useLiveRate();
  const [p, setP] = useState<Detail | null>(null);
  const [err, setErr] = useState("");
  const [open, setOpen] = useState<string | null>(null);
  const [added, setAdded] = useState<string[]>([]);

  // Re-price whenever the live rate changes (HTML is never trusted for prices).
  useEffect(() => { api<Detail>(`/products/${slug}`).then(setP).catch((e) => setErr(e.message)); }, [slug, rate?.sellPerGram18Rial]);

  if (err) return <p className="py-24 text-center text-muted-foreground">{err}</p>;
  if (!p) return <div className="mx-auto h-96 max-w-7xl animate-pulse px-4 py-10" />;

  return (
    <div className="mx-auto grid max-w-7xl gap-10 px-4 py-10 lg:grid-cols-2 [&>*]:min-w-0">
      <div className="lg:sticky lg:top-28 lg:self-start">
        <div className="relative aspect-[4/5] overflow-hidden rounded-[var(--radius)] bg-velvet">
          {p.imageUrl
            ? <Lens zoomFactor={2.2} lensSize={200} lensColor="black" ariaLabel="بزرگ‌نمایی عکس">
                <img src={hiRes(p.imageUrl)} alt={p.name} className="aspect-[4/5] w-full object-cover" />
              </Lens>
            : <Gem className="absolute inset-0 m-auto size-28 text-gold-hi/70" />}
        </div>
        {p.imageUrl && <p className="mt-3 text-center text-xs text-muted-foreground">برای دیدن جزئیات، نشانگر را روی عکس ببرید</p>}
      </div>
      <BlurFade className="space-y-6">
        <div>
          <p className="eyebrow">{p.categoryName}</p>
          <h1 className="mt-2 text-3xl font-light sm:text-5xl">{p.name}</h1>
          <p className="mt-3 leading-8 text-muted-foreground">{p.description}</p>
          <p className="mt-2 text-sm">{t("product.wage")}: {p.wageKind === 1 ? pct(p.wageValue) : `${toman(p.wageValue)} تومان/گرم`}</p>
        </div>
        <div className="flex flex-wrap gap-3">
          {site.contact.whatsApp && (
            <a href={waLink(site.contact.whatsApp, `سلام، «${p.name}» را از سایت دیدم و می‌خواهم سفارش بدهم.`)} target="_blank" rel="noopener noreferrer" data-testid="wa-order"
              className="inline-flex items-center gap-2 rounded-full bg-[#1f9d55] px-5 py-2.5 text-sm font-semibold text-white hover:brightness-110">
              <MessageCircle className="size-4" /> {t("wa.order")}
            </a>
          )}
          {site.installments?.enabled && p.pieces[0] && (
            <Link href={`/installments?amount=${Math.min(...p.pieces.map((x) => x.totalRial))}`}
              className="inline-flex items-center gap-2 rounded-full border border-primary/50 px-5 py-2.5 text-sm text-primary hover:bg-accent">
              <CalendarCheck className="size-4" /> خرید اقساطی این قطعه
            </Link>
          )}
        </div>
        {!p.canSellOnline && <p className="rounded-xl border border-amber-500/50 p-3 text-sm text-amber-700">{t("rate.paused")}</p>}
        <div className="rounded-[var(--radius)] border bg-card">
          <h2 className="border-b p-4 font-semibold">{t("product.pieces")}</h2>
          <div className="overflow-x-auto">
            <Table data-testid="pieces-table">
              <TableHeader><TableRow>
                <TableHead className="text-right">{t("product.branch")}</TableHead><TableHead className="text-right">{t("product.weight")}</TableHead>
                <TableHead className="text-right">{t("product.size")}</TableHead><TableHead className="text-right">{t("product.price")}</TableHead><TableHead />
              </TableRow></TableHeader>
              <TableBody>
                {p.pieces.map((x) => (
                  <Fragment key={x.pieceId}>
                    <TableRow className="cursor-pointer" onClick={() => setOpen(open === x.pieceId ? null : x.pieceId)}>
                      <TableCell>{x.branch}</TableCell>
                      <TableCell className="num">{grams(x.goldGrams)}</TableCell>
                      <TableCell>{x.size || "—"}</TableCell>
                      <TableCell className="num font-bold">{toman(x.totalRial)}</TableCell>
                      <TableCell>
                        {x.status !== "Available" ? <span className="text-xs text-muted-foreground">رزرو شده</span> :
                          <Button size="sm" disabled={!p.canSellOnline || added.includes(x.pieceId)} data-testid="add-to-cart"
                            onClick={(e) => { e.stopPropagation(); addToCart({ pieceId: x.pieceId, productName: p.name, slug: p.slug, grams: x.goldGrams, priceRial: x.totalRial }); setAdded([...added, x.pieceId]); }}>
                            {added.includes(x.pieceId) ? <Check /> : t("product.add")}
                          </Button>}
                      </TableCell>
                    </TableRow>
                    {open === x.pieceId && (
                      <TableRow><TableCell colSpan={5} className="bg-muted/40">
                        <div className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-3">
                          <span>طلای خام: <b className="num">{toman(x.rawGoldRial)}</b></span>
                          <span>اجرت: <b className="num">{toman(x.wageRial)}</b></span>
                          <span>سود: <b className="num">{toman(x.profitRial)}</b></span>
                          <span>مالیات: <b className="num">{toman(x.taxRial)}</b></span>
                          {x.stoneValueRial > 0 && <span>نگین: <b className="num">{toman(x.stoneValueRial)}</b></span>}
                          <span>عیار: <b className="num">{x.karat}</b></span>
                        </div>
                        <p className="mt-2 text-xs text-muted-foreground">{t("product.formula")}</p>
                      </TableCell></TableRow>
                    )}
                  </Fragment>
                ))}
              </TableBody>
            </Table>
          </div>
          <p className="border-t p-3 text-xs text-muted-foreground">برای دیدن {t("product.breakdown")} روی هر ردیف بزنید.</p>
        </div>
      </BlurFade>
    </div>
  );
}
