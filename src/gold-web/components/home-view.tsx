"use client";

import Link from "next/link";
import { useRef } from "react";
import { motion, useScroll, useTransform } from "framer-motion";
import { ArrowLeft, Receipt, RefreshCw, Wrench, Truck, ShieldCheck, Sparkles, Scale, MapPin } from "lucide-react";
import { useSite } from "./site-provider";
import { ZoomParallax } from "./slots/zoom-parallax";
import { HorizontalGallery } from "./gallery/horizontal-gallery";
import { ProductCardView, type ProductCard } from "./product-card";
import { useLiveRate } from "./rate-ticker";
import { OwnerSpotlight } from "./owner-spotlight";
import { InstallmentsBlock } from "./installments";
// 21st.dev @magicui primitives
import { TextAnimate } from "./ui21/text-animate";
import { BlurFade } from "./ui21/blur-fade";
import { Marquee } from "./ui21/marquee";
import { NumberTicker } from "./ui21/number-ticker";
import { BorderBeam } from "./ui21/border-beam";
import { ShimmerButton } from "./ui21/shimmer-button";

const ICONS: Record<string, React.ComponentType<{ className?: string; strokeWidth?: number }>> = {
  receipt: Receipt, refresh: RefreshCw, wrench: Wrench, truck: Truck, shield: ShieldCheck, sparkles: Sparkles,
};

type Cat = { id: string; slug: string; name: string };

export function HomeView({ products, categories }: { products: ProductCard[]; categories: Cat[] }) {
  const { site } = useSite();
  const hero = site.home.hero;
  const heroImages = hero.images ?? [];
  const blocks = site.home.blocks.filter((b) => b.enabled).sort((a, b) => a.order - b.order);
  const withImg = products.filter((p) => p.imageUrl);
  const featured = (withImg.filter((p) => p.isFeatured).length >= 4 ? withImg.filter((p) => p.isFeatured) : withImg).slice(0, 8);
  const catImage = (name: string) => withImg.find((p) => p.categoryName === name)?.imageUrl;

  const render: Record<string, (title: string, subtitle: string) => React.ReactNode> = {
    "rate-ticker": (title) => <RateBand title={title} />,
    categories: (title) => categories.length > 0 && (
      <section className="space-y-6">
        <Heading eyebrow="مجموعه‌ها" title={title} />
        <div className="relative" dir="ltr">
          <Marquee pauseOnHover className="[--duration:45s] [--gap:1rem]">
            {categories.map((c) => {
              const img = catImage(c.name);
              return (
                <Link key={c.id} href={`/shop?category=${c.slug}`} dir="rtl" className="group flex w-36 flex-col items-center gap-3 sm:w-52">
                  <div className="relative aspect-square w-full overflow-hidden rounded-full bg-velvet ring-1 ring-border transition-all duration-500 group-hover:ring-2 group-hover:ring-primary">
                    {img ? <img src={img} alt="" loading="lazy" className="size-full object-cover transition-transform duration-700 group-hover:scale-110" />
                      : <Sparkles className="absolute inset-0 m-auto size-10 text-gold-hi" />}
                  </div>
                  <span className="text-sm font-semibold transition-colors group-hover:text-primary">{c.name}</span>
                </Link>
              );
            })}
          </Marquee>
          <div className="pointer-events-none absolute inset-y-0 right-0 w-16 bg-gradient-to-l from-background" />
          <div className="pointer-events-none absolute inset-y-0 left-0 w-16 bg-gradient-to-r from-background" />
        </div>
      </section>
    ),
    "new-arrivals": (title) => products.length > 0 && (
      <section className="mx-auto max-w-7xl px-4">
        <Heading eyebrow="تازه‌های ویترین" title={title} link={{ href: "/shop?sort=newest", label: "مشاهده‌ی همه" }} />
        <div className="grid grid-cols-2 gap-x-3 gap-y-8 sm:gap-x-5 lg:grid-cols-4">
          {products.slice(0, 8).map((p, i) => <ProductCardView key={p.id} p={p} index={i} />)}
        </div>
      </section>
    ),
    gallery: (title, sub) => featured.length >= 3 && <HorizontalGallery eyebrow={sub || "کلکسیون"} title={title} items={featured} />,
    story: (title, sub) => <Story title={title} text={sub} image={heroImages[4] ?? heroImages[0]} />,
    "custom-order": (title, sub) => site.features.customOrder && (
      <Cta title={title} text={sub || "طرح یا عکس دلخواهت را بفرست؛ پیش‌فاکتور با وزن و اجرت شفاف می‌گیری."} href="/contact" icon={<Sparkles className="size-7" />} image={heroImages[5]} />
    ),
    "buy-back": (title, sub) => site.features.buyBack && (
      <Cta title={title} text={sub || "ارزش طلای کهنه‌ات را با نرخ روز حساب کن و نوبت ارزیابی حضوری بگیر."} href="/sell-old-gold" icon={<Scale className="size-7" />} image={heroImages[6]} />
    ),
    trust: (title) => site.trust.badges.length > 0 && (
      <section className="mx-auto max-w-7xl px-4">
        <Heading eyebrow="تعهد ما" title={title} />
        <div className="grid grid-cols-2 gap-px overflow-hidden rounded-[var(--radius)] border bg-border lg:grid-cols-4">
          {site.trust.badges.map((b, i) => { const I = ICONS[b.icon] ?? ShieldCheck; return (
            <BlurFade key={b.title} delay={i * 0.08} inView className="bg-card p-6 sm:p-8">
              <I className="mb-4 size-7 text-primary" strokeWidth={1.4} />
              <p className="font-semibold">{b.title}</p><p className="mt-1 text-sm leading-7 text-muted-foreground">{b.text}</p>
            </BlurFade>); })}
        </div>
      </section>
    ),
    testimonials: () => null,
    owner: (title, sub) => <OwnerSpotlight title={title} subtitle={sub} />,
    installments: (title) => <InstallmentsBlock title={title} />,
    contact: (title) => (site.contact.address || site.contact.phone) && (
      <section className="mx-auto max-w-7xl px-4">
        <Heading eyebrow="به ویترین ما سر بزنید" title={title} />
        <div className="flex flex-col gap-4 rounded-[var(--radius)] border bg-card p-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-start gap-3"><MapPin className="mt-1 size-5 text-primary" /><div><p>{site.contact.address}</p><p className="text-sm text-muted-foreground">{site.contact.workingHours}</p></div></div>
          {site.contact.phone && <a href={`tel:${site.contact.phone}`} className="num text-lg font-bold text-primary">{site.contact.phone}</a>}
        </div>
      </section>
    ),
  };

  return (
    <>
      <ZoomParallax images={heroImages.map((src) => ({ src }))}>
        <div className="max-w-4xl space-y-7">
          {site.brand.slogan && <p className="eyebrow">{site.brand.slogan}</p>}
          <TextAnimate as="h1" by="word" animation="blurInUp" once className="text-4xl font-extrabold leading-[1.35] text-velvet-foreground sm:text-6xl lg:text-7xl">
            {hero.title || site.brand.name}
          </TextAnimate>
          {hero.subtitle && <p className="mx-auto max-w-xl text-base leading-8 text-velvet-muted sm:text-lg">{hero.subtitle}</p>}
          {hero.ctaText && (
            <Link href={hero.ctaLink || "/shop"} className="inline-block">
              <ShimmerButton background="var(--brand-primary)" shimmerColor="#F3D58A" borderRadius="999px" className="px-8 py-3.5 font-semibold">
                {hero.ctaText} <ArrowLeft className="ms-2 size-4" />
              </ShimmerButton>
            </Link>
          )}
          <p className="text-xs tracking-widest text-velvet-muted">اسکرول کنید ↓</p>
        </div>
      </ZoomParallax>
      <div className="space-y-20 py-16 sm:space-y-28 sm:py-20">
        {blocks.map((b) => <div key={b.key}>{render[b.key]?.(b.title, b.subtitle)}</div>)}
      </div>
    </>
  );
}

function Heading({ eyebrow, title, link }: { eyebrow?: string; title: string; link?: { href: string; label: string } }) {
  return (
    <BlurFade inView className="mx-auto mb-8 flex max-w-7xl items-end justify-between gap-4 px-4 sm:mb-10">
      <div>
        {eyebrow && <p className="eyebrow">{eyebrow}</p>}
        <h2 className="mt-2 text-3xl font-light sm:text-5xl">{title}</h2>
      </div>
      {link && <Link href={link.href} className="group flex shrink-0 items-center gap-1 border-b border-primary/40 pb-1 text-sm text-primary">{link.label}<ArrowLeft className="size-4 transition-transform group-hover:-translate-x-1" /></Link>}
    </BlurFade>
  );
}

function RateBand({ title }: { title: string }) {
  const rate = useLiveRate();
  if (!rate) return null;
  return (
    <section className="mx-auto max-w-7xl px-4">
      <div className="relative overflow-hidden rounded-[var(--radius)] border bg-card px-6 py-8 sm:px-10">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="eyebrow flex items-center gap-2">
              <span className="relative flex size-2"><span className={`absolute inline-flex size-full animate-ping rounded-full opacity-60 ${rate.canSellOnline ? "bg-emerald-500" : "bg-amber-500"}`} /><span className={`relative inline-flex size-2 rounded-full ${rate.canSellOnline ? "bg-emerald-500" : "bg-amber-500"}`} /></span>
              زنده
            </p>
            <h2 className="mt-2 text-2xl font-light sm:text-3xl">{title}</h2>
            <p className="mt-1 text-sm text-muted-foreground">هر گرم طلای ۱۸ عیار — قیمت همه‌ی محصولات با همین نرخ ساخته می‌شود.</p>
          </div>
          <div className="flex items-baseline gap-2">
            <NumberTicker value={Math.round(rate.sellPerGram18Rial / 10)} locale="fa-IR" className="num text-4xl font-extrabold text-primary sm:text-6xl" />
            <span className="text-muted-foreground">تومان</span>
          </div>
        </div>
        <Link href="/price" className="mt-6 inline-flex items-center gap-1 text-sm text-primary">محاسبه‌گر قیمت <ArrowLeft className="size-4" /></Link>
        <BorderBeam size={140} duration={10} colorFrom="#F3D58A" colorTo="#8E6B2F" />
      </div>
    </section>
  );
}

function Story({ title, text, image }: { title: string; text: string; image?: string }) {
  const ref = useRef<HTMLElement>(null);
  const { scrollYProgress } = useScroll({ target: ref, offset: ["start end", "end start"] });
  const y = useTransform(scrollYProgress, [0, 1], ["-12%", "12%"]);
  return (
    <section ref={ref} className="mx-auto grid max-w-7xl items-center gap-10 px-4 lg:grid-cols-2">
      <div className="relative aspect-[4/5] overflow-hidden rounded-[var(--radius)] bg-velvet">
        {image && <motion.img style={{ y }} src={image} alt="" loading="lazy" className="absolute inset-0 h-[124%] w-full -translate-y-[12%] object-cover" />}
      </div>
      <BlurFade inView className="space-y-6">
        <p className="eyebrow">داستان ما</p>
        <h2 className="text-3xl font-light leading-[1.5] sm:text-5xl">{title}</h2>
        {text && <p className="max-w-lg leading-8 text-muted-foreground">{text}</p>}
        <Link href="/shop" className="group inline-flex items-center gap-2 border-b border-primary pb-1 text-primary">دیدن مجموعه <ArrowLeft className="size-4 transition-transform group-hover:-translate-x-1" /></Link>
      </BlurFade>
    </section>
  );
}

function Cta({ title, text, href, icon, image }: { title: string; text: string; href: string; icon: React.ReactNode; image?: string }) {
  return (
    <BlurFade inView className="mx-auto max-w-7xl px-4">
      <Link href={href} className="velvet group relative flex min-h-64 flex-col justify-end gap-4 overflow-hidden rounded-[var(--radius)] p-8 sm:p-12">
        {image && <img src={image} alt="" loading="lazy" className="absolute inset-0 size-full object-cover opacity-80 transition-all duration-[1.2s] group-hover:scale-105 group-hover:opacity-100" />}
        <div className="absolute inset-0 bg-gradient-to-l from-velvet via-velvet/60 to-transparent" />
        <div className="relative max-w-xl space-y-3">
          <div className="text-gold-hi">{icon}</div>
          <h2 className="text-3xl font-light sm:text-4xl">{title}</h2>
          <p className="leading-8 text-velvet-muted">{text}</p>
          <span className="inline-flex items-center gap-2 text-gold-hi">شروع کنید <ArrowLeft className="size-4 transition-transform group-hover:-translate-x-2" /></span>
        </div>
      </Link>
    </BlurFade>
  );
}
