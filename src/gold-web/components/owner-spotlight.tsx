"use client";

/**
 * "Meet the owner" — for shops whose owner is the face of the brand.
 * Photo + quote + highlights, his own short videos (uploaded MP4 or Instagram reel, loaded on click),
 * and his gold-buying tips. Motion: 21st @magicui BlurFade + BorderBeam. Everything comes from admin → «مشاور / مدیر».
 */
import { useEffect, useState } from "react";
import { Check, AtSign as Instagram, MessageCircle, Play, Lightbulb } from "lucide-react";
import { useSite } from "./site-provider";
import { BlurFade } from "./ui21/blur-fade";
import { BorderBeam } from "./ui21/border-beam";
import { waLink } from "@/lib/utils";

type Reel = { title: string; instagramUrl: string; videoUrl: string; posterUrl: string };

const reelId = (url: string) => url.match(/instagram\.com\/(?:[\w.]+\/)?(?:reel|p)\/([\w-]+)/)?.[1];

export function OwnerSpotlight({ title, subtitle }: { title: string; subtitle: string }) {
  const { site } = useSite();
  const o = site.owner;
  // Probe the photo in the browser: until it really loads we show the monogram (no broken-image icon).
  const [photoOk, setPhotoOk] = useState(false);
  useEffect(() => {
    if (!o.photoUrl) return;
    const img = new Image();
    img.onload = () => setPhotoOk(true);
    img.src = o.photoUrl;
  }, [o.photoUrl]);
  if (!o.enabled || !o.name) return null;
  const wa = site.contact.whatsApp || site.contact.mobile;

  return (
    <section className="velvet relative overflow-hidden py-20 sm:py-28" data-testid="owner">
      <div aria-hidden className="pointer-events-none absolute -top-40 left-1/2 size-[70vmax] -translate-x-1/2 rounded-full bg-[radial-gradient(circle,color-mix(in_srgb,var(--brand-primary)_22%,transparent),transparent_60%)]" />
      <div className="relative mx-auto grid max-w-7xl items-center gap-12 px-4 lg:grid-cols-[5fr_7fr]">
        <BlurFade inView className="relative mx-auto w-full max-w-md">
          <div className="relative aspect-[4/5] overflow-hidden rounded-[calc(var(--radius)*1.5)] border border-white/10 bg-[linear-gradient(160deg,#2a2016,#0f0b08)] shadow-[0_40px_120px_-30px_rgba(0,0,0,.8)]">
            {o.photoUrl && photoOk
              ? <img src={o.photoUrl} alt={o.name} className="size-full object-cover" />
              : <div className="grid size-full place-items-center">
                  <div className="text-center">
                    <div className="mx-auto grid size-40 place-items-center rounded-full border border-gold-hi/40 text-7xl font-extrabold gold-text">{(o.name.trim().split(" ").slice(-1)[0] || o.name)[0]}</div>
                    <p className="mt-6 text-sm text-velvet-muted">عکس آقای {o.name.split(" ").slice(-1)[0]} را از پنل بگذارید</p>
                  </div>
                </div>}
            <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/85 via-black/30 to-transparent p-6 pt-24">
              <p className="text-2xl font-extrabold text-white">{o.name}</p>
              <p className="text-sm text-white/75">{o.role}</p>
            </div>
            <BorderBeam size={160} duration={12} colorFrom="#F3D58A" colorTo="#8C6A2E" />
          </div>
          {o.followers && (
            <div className="absolute -bottom-5 left-4 flex items-center gap-2 rounded-full border border-white/10 bg-black/70 px-4 py-2 text-sm text-white backdrop-blur">
              <Instagram className="size-4 text-gold-hi" /><span className="font-bold">{o.followers}</span><span className="text-white/70">دنبال‌کننده</span>
            </div>
          )}
        </BlurFade>

        <BlurFade inView delay={0.1} className="space-y-7">
          <div>
            <p className="eyebrow">{subtitle || "مشاور شما"}</p>
            <h2 className="mt-3 text-3xl font-light leading-[1.4] sm:text-5xl">{title}</h2>
          </div>
          {o.quote && (
            <blockquote className="border-r-2 border-gold-hi/70 pr-5 text-xl leading-9 text-velvet-foreground/90 sm:text-2xl">«{o.quote}»</blockquote>
          )}
          {o.highlights.length > 0 && (
            <ul className="grid gap-3 sm:grid-cols-2">
              {o.highlights.map((h) => (
                <li key={h} className="flex items-start gap-3 text-velvet-foreground/85">
                  <span className="mt-1 grid size-5 shrink-0 place-items-center rounded-full bg-gold-hi/15"><Check className="size-3.5 text-gold-hi" /></span>{h}
                </li>
              ))}
            </ul>
          )}
          <div className="flex flex-wrap gap-3">
            {wa && (
              <a href={waLink(wa, `سلام آقای ${o.name.split(" ").slice(-1)[0]}، برای مشاوره‌ی خرید طلا پیام می‌دهم.`)} target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-2 rounded-full bg-[#1f9d55] px-6 py-3 font-semibold text-white transition hover:brightness-110" data-testid="owner-whatsapp">
                <MessageCircle className="size-5" /> {o.ctaText || "مشاوره در واتساپ"}
              </a>
            )}
            {o.instagram && (
              <a href={`https://instagram.com/${o.instagram}`} target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-2 rounded-full border border-white/20 px-6 py-3 text-velvet-foreground transition hover:border-gold-hi hover:text-gold-hi">
                <Instagram className="size-5" /> <span dir="ltr">@{o.instagram}</span>
              </a>
            )}
          </div>
        </BlurFade>
      </div>

      {o.reels.length > 0 && (
        <div className="relative mx-auto mt-20 max-w-7xl px-4">
          <div className="mb-6 flex items-end justify-between gap-4">
            <div><p className="eyebrow">ویدیوهای آقای {o.name.split(" ").slice(-1)[0]}</p><h3 className="mt-2 text-2xl font-light sm:text-3xl">حرف‌های رو در رو</h3></div>
          </div>
          <div className="flex snap-x snap-mandatory gap-4 overflow-x-auto pb-4 [scrollbar-width:none]">
            {o.reels.map((r, i) => <ReelCard key={r.title + i} r={r} photo={photoOk ? o.photoUrl : ""} />)}
          </div>
          <p className="mt-2 text-xs text-velvet-muted">ویدیوهای اینستاگرامی با زدن روی هر کارت بارگذاری می‌شوند. ویدیوی آپلودشده در پنل، بدون اینستاگرام هم پخش می‌شود.</p>
        </div>
      )}

      {o.tips.length > 0 && (
        <div className="relative mx-auto mt-20 max-w-7xl px-4">
          <p className="eyebrow">نکته‌های طلایی</p>
          <h3 className="mt-2 text-2xl font-light sm:text-3xl">قبل از خرید، این‌ها را بدانید</h3>
          <div className="mt-8 grid gap-px overflow-hidden rounded-[var(--radius)] border border-white/10 bg-white/10 sm:grid-cols-2 lg:grid-cols-3">
            {o.tips.map((t, i) => (
              <BlurFade key={t.title} inView delay={i * 0.06} className="bg-velvet p-6 sm:p-8">
                <div className="mb-4 flex items-center gap-3">
                  <span className="num grid size-9 place-items-center rounded-full border border-gold-hi/40 text-sm text-gold-hi">{String(i + 1).padStart(2, "0")}</span>
                  <Lightbulb className="size-4 text-gold-hi/70" />
                </div>
                <p className="text-lg font-semibold">{t.title}</p>
                <p className="mt-2 leading-7 text-velvet-muted">{t.text}</p>
              </BlurFade>
            ))}
          </div>
        </div>
      )}
    </section>
  );
}

function ReelCard({ r, photo }: { r: Reel; photo: string }) {
  const [on, setOn] = useState(false);
  const id = reelId(r.instagramUrl);
  const poster = r.posterUrl || photo;
  return (
    <div className="relative aspect-[9/16] w-56 shrink-0 snap-start overflow-hidden rounded-[var(--radius)] border border-white/10 bg-[linear-gradient(160deg,#3a2c1b,#0f0b08)] sm:w-64">
      {on && r.videoUrl ? (
        <video src={r.videoUrl} controls autoPlay playsInline className="size-full object-cover" />
      ) : on && id ? (
        <iframe src={`https://www.instagram.com/reel/${id}/embed/`} title={r.title} className="size-full bg-white" loading="lazy" allow="autoplay; encrypted-media" />
      ) : (
        <button type="button" onClick={() => setOn(true)} className="group absolute inset-0 text-right" aria-label={`پخش: ${r.title}`}>
          {poster && <img src={poster} alt="" className="absolute inset-0 size-full object-cover opacity-50 transition duration-700 group-hover:scale-105 group-hover:opacity-70" />}
          <div className="absolute inset-0 bg-gradient-to-t from-black/90 via-black/20 to-black/30" />
          <span className="absolute top-1/2 left-1/2 grid size-16 -translate-x-1/2 -translate-y-1/2 place-items-center rounded-full bg-white/15 backdrop-blur transition group-hover:scale-110 group-hover:bg-gold-hi/80">
            <Play className="size-7 translate-x-0.5 fill-white text-white" />
          </span>
          <span className="absolute inset-x-0 bottom-0 p-4 text-sm font-semibold leading-7 text-white">{r.title}</span>
        </button>
      )}
    </div>
  );
}
