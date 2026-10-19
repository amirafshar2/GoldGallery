"use client";

import Link from "next/link";
import { Gem } from "lucide-react";
import { toman, grams, pct } from "@/lib/format";
import { useSite } from "./site-provider";
import { BlurFade } from "./ui21/blur-fade";
import { BorderBeam } from "./ui21/border-beam";

export type ProductCard = {
  id: string; slug: string; name: string; categoryName: string; imageUrl: string; wageBp: number;
  startingFromRial: number | null; lightestGrams: number | null; availablePieces: number; isFeatured: boolean;
};

/** Editorial product card: velvet photo plate (4:5) + ivory caption. Motion: 21st @magicui BlurFade + BorderBeam. */
export function ProductCardView({ p, index = 0 }: { p: ProductCard; index?: number }) {
  const { t } = useSite();
  return (
    <BlurFade delay={0.04 * (index % 8)} inView inViewMargin="-60px">
      <Link href={`/shop/${p.slug}`} data-testid="product-card" className="group flex flex-col gap-3">
        <div className="relative aspect-[4/5] overflow-hidden rounded-[var(--radius)] bg-velvet">
          {p.imageUrl
            ? <img src={p.imageUrl} alt={p.name} loading="lazy"
                className="size-full object-cover transition-[transform,filter] duration-[1.1s] ease-out group-hover:scale-[1.08] group-hover:brightness-110" />
            : <Gem className="absolute inset-0 m-auto size-16 text-gold-hi/70 transition-transform duration-700 group-hover:scale-110 group-hover:rotate-6" />}
          <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/45 via-transparent to-transparent opacity-0 transition-opacity duration-500 group-hover:opacity-100" />
          <div className="absolute inset-x-3 bottom-3 flex translate-y-3 items-center justify-between rounded-full bg-white/90 px-4 py-2 text-xs font-semibold text-[#1D1812] opacity-0 backdrop-blur transition-all duration-500 group-hover:translate-y-0 group-hover:opacity-100">
            <span>مشاهده و قیمت لحظه‌ای</span><span aria-hidden>←</span>
          </div>
          {p.wageBp > 0 && p.wageBp <= 1000 && <span className="absolute top-3 right-3 rounded-full bg-primary px-2.5 py-0.5 text-[11px] text-primary-foreground">کم‌اجرت</span>}
          {p.isFeatured && <BorderBeam size={90} duration={9} colorFrom="#F3D58A" colorTo="#8E6B2F" className="opacity-0 transition-opacity duration-500 group-hover:opacity-100" />}
        </div>
        <div className="flex items-start justify-between gap-3 px-1">
          <div className="min-w-0">
            <p className="text-[11px] tracking-widest text-muted-foreground">{p.categoryName}</p>
            <h3 className="mt-0.5 truncate font-semibold leading-7">{p.name}</h3>
            <p className="text-sm">
              <span className="text-muted-foreground">{t("card.from")} </span>
              <span className="num font-bold text-primary">{toman(p.startingFromRial)}</span>
              <span className="text-xs text-muted-foreground"> تومان</span>
            </p>
          </div>
          <div className="shrink-0 text-left text-[11px] leading-6 text-muted-foreground">
            {p.lightestGrams != null && <p>{grams(p.lightestGrams)} گرم</p>}
            {p.wageBp > 0 && <p>{t("card.wage")} {pct(p.wageBp)}</p>}
          </div>
        </div>
      </Link>
    </BlurFade>
  );
}
