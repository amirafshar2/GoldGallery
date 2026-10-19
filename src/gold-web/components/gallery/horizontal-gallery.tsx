"use client";

/**
 * Horizontal scroll gallery — pattern from 21st.dev "Horizontal Scroll" / "Immersive Scroll Gallery":
 * vertical scroll drives a sticky horizontal track. RTL-aware (track moves toward the reading direction).
 * Phones get a native swipe carousel with snap instead (no scroll-jacking on touch).
 */
import Link from "next/link";
import { useLayoutEffect, useRef, useState } from "react";
import { motion, useScroll, useTransform, useReducedMotion } from "framer-motion";
import { toman } from "@/lib/format";
import type { ProductCard } from "../product-card";

export function HorizontalGallery({ eyebrow, title, items }: { eyebrow: string; title: string; items: ProductCard[] }) {
  const ref = useRef<HTMLElement>(null);
  const track = useRef<HTMLDivElement>(null);
  const [dist, setDist] = useState(0);
  const reduce = useReducedMotion();

  useLayoutEffect(() => {
    const m = () => setDist(Math.max(0, (track.current?.scrollWidth ?? 0) - window.innerWidth + 64));
    m(); window.addEventListener("resize", m);
    return () => window.removeEventListener("resize", m);
  }, [items.length]);

  const { scrollYProgress } = useScroll({ target: ref, offset: ["start start", "end end"] });
  const x = useTransform(scrollYProgress, [0, 1], [0, dist]); // RTL: content overflows to the left → move right

  if (items.length === 0) return null;
  const Card = ({ p, i }: { p: ProductCard; i: number }) => (
    <Link href={`/shop/${p.slug}`} className="group relative block shrink-0 snap-center overflow-hidden rounded-sm w-[78vw] sm:w-[46vw] md:w-[min(32vw,calc((100vh-300px)*0.8))]">
      <div className="relative aspect-[4/5] overflow-hidden">
        <img src={p.imageUrl} alt={p.name} loading="lazy" className="size-full object-cover transition-transform duration-[1.2s] ease-out group-hover:scale-110" />
        <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/10 to-transparent" />
        <span className="num absolute top-4 left-4 text-xs tracking-[.3em] text-white/70">{String(i + 1).padStart(2, "0")}</span>
        <div className="absolute inset-x-0 bottom-0 p-5 text-white">
          <p className="text-xs text-white/70">{p.categoryName}</p>
          <h3 className="mt-1 text-xl font-semibold">{p.name}</h3>
          <p className="mt-2 text-sm text-white/80">از <span className="num">{toman(p.startingFromRial)}</span> تومان</p>
        </div>
      </div>
    </Link>
  );

  return (
    <section ref={ref} className="velvet relative md:h-[260vh]">
      <div className="md:sticky md:top-0 md:flex md:h-screen md:flex-col md:justify-center md:overflow-hidden py-16 md:pt-28 md:pb-8">
        <div className="mx-auto mb-6 w-full max-w-7xl px-4">
          <p className="eyebrow">{eyebrow}</p>
          <h2 className="mt-2 text-3xl font-light sm:text-5xl">{title}</h2>
        </div>
        {/* phones: swipe */}
        <div className="flex snap-x snap-mandatory gap-4 overflow-x-auto px-4 pb-4 [scrollbar-width:none] md:hidden">
          {items.map((p, i) => <Card key={p.id} p={p} i={i} />)}
        </div>
        {/* desktop: scroll-driven */}
        <motion.div ref={track} style={reduce ? undefined : { x }} className={`hidden gap-6 px-8 will-change-transform md:flex ${reduce ? "overflow-x-auto" : ""}`}>
          {items.map((p, i) => <Card key={p.id} p={p} i={i} />)}
        </motion.div>
      </div>
    </section>
  );
}
