"use client";

import { use, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowUp, ArrowDown, Plus, Trash2, ExternalLink } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import type { Site } from "@/lib/site";
import { DEFAULT_TEXTS } from "@/lib/texts";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { TextField, ColorField, Toggle, ImageField, ImageList, swap } from "@/components/admin/fields";

const TITLES: Record<string, [string, string]> = {
  brand: ["نام و لوگو", "این‌ها بالای همه‌ی صفحه‌ها و در تب مرورگر دیده می‌شوند."],
  theme: ["رنگ و ظاهر", "رنگ‌ها فوراً روی کل سایت اعمال می‌شوند. رنگ طلایی لوگو را برای «رنگ اصلی» بگذارید."],
  contact: ["تماس و آدرس", "در فوتر، صفحه‌ی تماس و بخش «ویترین» صفحه‌ی اول نمایش داده می‌شود."],
  legal: ["مجوزها و درباره‌ی ما", "پروانه‌ی کسب و کد اقتصادی اعتماد مشتری را بالا می‌برد."],
  home: ["صفحه‌ی اول", "عنوان و عکس‌های اسکرولی بالای صفحه + ترتیب و روشن/خاموش بخش‌ها."],
  trust: ["مزیت‌ها (چرا ما؟)", "کارت‌های کوچک اعتمادسازی مثل «فاکتور رسمی» و «تعویض ۴ روزه»."],
  footer: ["فوتر", "متن و لینک‌های پایین صفحه."],
  seo: ["سئو", "عنوان و توضیحی که در گوگل و هنگام اشتراک لینک نمایش داده می‌شود."],
  features: ["بخش‌های فعال", "هر بخشی را که لازم ندارید خاموش کنید؛ از منو و صفحه‌ی اول حذف می‌شود."],
  texts: ["همه‌ی متن‌های سایت", "هر برچسب سایت را می‌توانید عوض کنید. خالی بگذارید تا متن پیش‌فرض نمایش داده شود."],
  publish: ["انتشار سایت", "تا وقتی منتشر نشده، بازدیدکننده‌ها فقط پیام «به‌زودی» را می‌بینند."],
  owner: ["مدیر / مشاور (چهره‌ی برند)", "اگر صاحب مغازه جلوی دوربین است، اینجا عکس، جمله، ویدیوها و نکته‌هایش را بگذارید."],
  installments: ["خرید اقساطی", "شرایط اقساط روی ماشین‌حساب صفحه‌ی اول و صفحه‌ی «خرید اقساطی» اعمال می‌شود."],
};
const ICONS = ["receipt", "refresh", "wrench", "truck", "shield", "sparkles"];
const BLOCK_NAMES: Record<string, string> = { "rate-ticker": "نوار قیمت لحظه‌ای", "new-arrivals": "جدیدترین‌ها", categories: "دسته‌بندی‌ها", gallery: "گالری اسکرولی (منتخب‌ها)", owner: "مدیر / مشاور", installments: "ماشین‌حساب اقساط", story: "روایت برند (عکس + متن)", "custom-order": "سفارش ساخت", "buy-back": "خرید طلای کهنه", trust: "مزیت‌ها", testimonials: "رضایت مشتریان", contact: "ویترین و آدرس" };
const FEATURES: [string, string][] = [["shop", "فروشگاه آنلاین"], ["nameNecklace", "پلاک اسم"], ["customOrder", "سفارش ساخت"], ["buyBack", "خرید طلای کهنه"], ["bridal", "سرویس عروس"], ["wholesale", "پورتال عمده"], ["installments", "خرید اقساطی"], ["blog", "وبلاگ"], ["repairs", "تعمیرات"]];

/* eslint-disable @typescript-eslint/no-explicit-any */
export default function SectionEditor({ params }: { params: Promise<{ section: string }> }) {
  const { section } = use(params);
  const [v, setV] = useState<any>(null);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [saving, setSaving] = useState(false);
  const router = useRouter();

  useEffect(() => { setV(null); api<Site>("/site").then((s) => setV(structuredClone((s as any)[section]))); }, [section]);
  if (!TITLES[section]) return <p>بخش ناشناخته.</p>;
  if (!v) return <p className="text-muted-foreground">در حال بارگذاری…</p>;

  const set = (k: string, val: unknown) => setV({ ...v, [k]: val });
  const save = async () => {
    setSaving(true); setMsg(null);
    try { await api(`/admin/site/${section}`, { method: "PUT", json: v }); setMsg({ ok: true, text: "ذخیره شد ✓" }); window.dispatchEvent(new Event("site-saved")); router.refresh(); }
    catch (e) { setMsg({ ok: false, text: (e as ApiError).message }); } finally { setSaving(false); }
  };

  const body: Record<string, () => React.ReactNode> = {
    brand: () => <>
      <TextField label="نام فروشگاه" value={v.name} onChange={(x) => set("name", x)} placeholder="مثلاً گالری طلای آفتاب" />
      <TextField label="شعار" value={v.slogan} onChange={(x) => set("slogan", x)} />
      <ImageField label="لوگو" hint="PNG با پس‌زمینه‌ی شفاف بهترین نتیجه را دارد." value={v.logoUrl} onChange={(x) => set("logoUrl", x)} />
      <ImageField label="آیکون تب مرورگر (اختیاری)" value={v.faviconUrl} onChange={(x) => set("faviconUrl", x)} />
    </>,
    theme: () => <div className="grid gap-4 sm:grid-cols-2">
      <ColorField label="رنگ اصلی (طلایی برند)" value={v.primary} onChange={(x) => set("primary", x)} />
      <ColorField label="پس‌زمینه" value={v.background} onChange={(x) => set("background", x)} />
      <ColorField label="کارت‌ها" value={v.surface} onChange={(x) => set("surface", x)} />
      <ColorField label="متن" value={v.text} onChange={(x) => set("text", x)} />
      <ColorField label="متن کم‌رنگ" value={v.muted} onChange={(x) => set("muted", x)} />
      <TextField label="گردی گوشه‌ها" value={v.radius} ltr onChange={(x) => set("radius", x)} hint="مثلاً 16px" />
      <div className="flex gap-2 sm:col-span-2">
        <Button type="button" variant="outline" onClick={() => setV({ ...v, mode: "dark", background: "#0B0A08", surface: "#15130F", text: "#F4EFE6", primary: "#C9A24A", muted: "#B8AD99" })}>تم مخمل تیره</Button>
        <Button type="button" variant="outline" onClick={() => setV({ ...v, mode: "light", background: "#F6F1E9", surface: "#FFFDF9", text: "#1D1812", primary: "#8E6B2F", muted: "#6F6556" })}>تم عاج و مخمل (پیش‌فرض)</Button>
      </div>
    </div>,
    contact: () => <div className="grid gap-4 sm:grid-cols-2">
      {([["phone", "تلفن ثابت"], ["mobile", "موبایل"], ["whatsApp", "واتساپ"], ["instagram", "اینستاگرام (بدون @)"], ["telegram", "تلگرام"], ["email", "ایمیل"], ["city", "شهر"], ["workingHours", "ساعات کاری"], ["mapUrl", "لینک نقشه (نشان/بلد/گوگل)"]] as const)
        .map(([k, l]) => <TextField key={k} label={l} value={v[k]} ltr={["phone", "mobile", "whatsApp", "email", "mapUrl", "instagram", "telegram"].includes(k)} onChange={(x) => set(k, x)} />)}
      <div className="sm:col-span-2"><TextField label="آدرس کامل" multiline value={v.address} onChange={(x) => set("address", x)} /></div>
    </div>,
    legal: () => <>
      <TextField label="شماره پروانه‌ی کسب" value={v.licenseNumber} onChange={(x) => set("licenseNumber", x)} />
      <TextField label="کد اقتصادی" value={v.economicCode} onChange={(x) => set("economicCode", x)} />
      <ImageField label="تصویر نماد اعتماد (اختیاری)" value={v.trustSealUrl} onChange={(x) => set("trustSealUrl", x)} />
      <TextField label="درباره‌ی ما" multiline value={v.aboutText} onChange={(x) => set("aboutText", x)} />
    </>,
    home: () => <>
      <TextField label="عنوان بزرگ بالای صفحه" value={v.hero.title} onChange={(x) => set("hero", { ...v.hero, title: x })} />
      <TextField label="زیرعنوان" value={v.hero.subtitle} onChange={(x) => set("hero", { ...v.hero, subtitle: x })} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="متن دکمه" value={v.hero.ctaText} onChange={(x) => set("hero", { ...v.hero, ctaText: x })} />
        <TextField label="لینک دکمه" ltr value={v.hero.ctaLink} onChange={(x) => set("hero", { ...v.hero, ctaLink: x })} />
      </div>
      <ImageList label="عکس‌های اسکرولی (تا ۷ عکس)" hint="عکس اول وسط صفحه است و با اسکرول تمام‌صفحه می‌شود؛ بقیه دورش زوم می‌شوند. عکس‌های ماکرو و تاریک بهترین جلوه را دارند." value={v.hero.images ?? []} onChange={(x) => set("hero", { ...v.hero, images: x })} />
      <div className="space-y-2">
        <p className="text-sm font-semibold">بخش‌های صفحه‌ی اول (ترتیب و روشن/خاموش)</p>
        {v.blocks.map((b: any, i: number) => (
          <div key={b.key} className="flex flex-wrap items-center gap-2 rounded-xl border p-2">
            <div className="flex flex-col">
              <button type="button" onClick={() => i > 0 && set("blocks", swap(v.blocks, i, i - 1).map((x: any, k: number) => ({ ...x, order: k + 1 })))} aria-label="بالا"><ArrowUp className="size-4" /></button>
              <button type="button" onClick={() => i < v.blocks.length - 1 && set("blocks", swap(v.blocks, i, i + 1).map((x: any, k: number) => ({ ...x, order: k + 1 })))} aria-label="پایین"><ArrowDown className="size-4" /></button>
            </div>
            <span className="w-32 text-xs text-muted-foreground">{BLOCK_NAMES[b.key] ?? b.key}</span>
            <Input className="min-w-40 flex-1" value={b.title} onChange={(e) => set("blocks", v.blocks.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
            <button type="button" onClick={() => set("blocks", v.blocks.map((x: any, k: number) => (k === i ? { ...x, enabled: !x.enabled } : x)))}
              className={`rounded-full px-3 py-1 text-xs ${b.enabled ? "bg-primary text-primary-foreground" : "bg-muted"}`}>{b.enabled ? "روشن" : "خاموش"}</button>
          </div>
        ))}
      </div>
    </>,
    trust: () => <>
      {v.badges.map((b: any, i: number) => (
        <div key={i} className="grid gap-2 rounded-xl border p-3 sm:grid-cols-[120px_1fr_2fr_auto]">
          <select value={b.icon} onChange={(e) => set("badges", v.badges.map((x: any, k: number) => (k === i ? { ...x, icon: e.target.value } : x)))} className="h-9 rounded-md border bg-background px-2 text-sm">
            {ICONS.map((ic) => <option key={ic}>{ic}</option>)}
          </select>
          <Input placeholder="عنوان" value={b.title} onChange={(e) => set("badges", v.badges.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
          <Input placeholder="توضیح" value={b.text} onChange={(e) => set("badges", v.badges.map((x: any, k: number) => (k === i ? { ...x, text: e.target.value } : x)))} />
          <Button type="button" variant="ghost" onClick={() => set("badges", v.badges.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
        </div>
      ))}
      <Button type="button" variant="outline" onClick={() => set("badges", [...v.badges, { icon: "shield", title: "", text: "" }])}><Plus /> افزودن مزیت</Button>
    </>,
    footer: () => <>
      <TextField label="متن فوتر" multiline value={v.text} onChange={(x) => set("text", x)} />
      {v.links.map((l: any, i: number) => (
        <div key={i} className="flex gap-2">
          <Input placeholder="عنوان" value={l.title} onChange={(e) => set("links", v.links.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
          <Input placeholder="/terms" dir="ltr" value={l.url} onChange={(e) => set("links", v.links.map((x: any, k: number) => (k === i ? { ...x, url: e.target.value } : x)))} />
          <Button type="button" variant="ghost" onClick={() => set("links", v.links.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
        </div>
      ))}
      <Button type="button" variant="outline" onClick={() => set("links", [...v.links, { title: "", url: "/" }])}><Plus /> افزودن لینک</Button>
    </>,
    seo: () => <>
      <TextField label="عنوان در گوگل" value={v.title} onChange={(x) => set("title", x)} />
      <TextField label="توضیح در گوگل" multiline value={v.description} onChange={(x) => set("description", x)} />
      <ImageField label="تصویر اشتراک‌گذاری" value={v.ogImageUrl} onChange={(x) => set("ogImageUrl", x)} />
    </>,
    features: () => <div className="grid gap-2 sm:grid-cols-2">{FEATURES.map(([k, l]) => <Toggle key={k} label={l} value={!!v[k]} onChange={(x) => set(k, x)} />)}</div>,
    texts: () => <div className="grid gap-3">
      {Object.entries(DEFAULT_TEXTS).map(([k, def]) => (
        <div key={k} className="grid gap-1 sm:grid-cols-[200px_1fr] sm:items-center">
          <code className="text-xs text-muted-foreground" dir="ltr">{k}</code>
          <Input data-testid={`text-${k}`} placeholder={def} value={v.values?.[k] ?? ""} onChange={(e) => set("values", { ...v.values, [k]: e.target.value })} />
        </div>
      ))}
    </div>,
    owner: () => <>
      <Toggle label="این بخش در صفحه‌ی اول نمایش داده شود" value={v.enabled} onChange={(x) => set("enabled", x)} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="نام" value={v.name} onChange={(x) => set("name", x)} placeholder="مثلاً اسماعیل مروّجی" />
        <TextField label="سمت" value={v.role} onChange={(x) => set("role", x)} />
        <TextField label="اینستاگرام شخصی (بدون @)" ltr value={v.instagram} onChange={(x) => set("instagram", x)} />
        <TextField label="تعداد دنبال‌کننده (اختیاری)" value={v.followers} onChange={(x) => set("followers", x)} />
      </div>
      <ImageField label="عکس (عمودی، پرتره)" hint="بهترین نتیجه: عکس عمودی با پس‌زمینه‌ی تیره و نور گرم." value={v.photoUrl} onChange={(x) => set("photoUrl", x)} />
      <TextField label="جمله‌ی معروف" multiline value={v.quote} onChange={(x) => set("quote", x)} />
      <TextField label="متن دکمه‌ی مشاوره" value={v.ctaText} onChange={(x) => set("ctaText", x)} />
      <div className="space-y-2">
        <p className="text-sm font-semibold">ویژگی‌ها (هر خط یکی)</p>
        <textarea className="min-h-24 w-full rounded-md border bg-background p-2 text-sm" value={(v.highlights ?? []).join("\n")} onChange={(e) => set("highlights", e.target.value.split("\n").filter((x) => x.trim()))} />
      </div>
      <div className="space-y-2">
        <p className="text-sm font-semibold">ویدیوها</p>
        <p className="text-xs text-muted-foreground">لینک ریلز اینستاگرام بگذارید، یا برای پخش بدون فیلتر، فایل MP4 (تا ۲۵ مگابایت) آپلود کنید.</p>
        {v.reels.map((r: any, i: number) => (
          <div key={i} className="grid gap-2 rounded-xl border p-3">
            <div className="flex gap-2">
              <Input placeholder="عنوان ویدیو" value={r.title} onChange={(e) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
              <Button type="button" variant="ghost" onClick={() => set("reels", v.reels.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
            </div>
            <Input placeholder="https://www.instagram.com/reel/…" dir="ltr" value={r.instagramUrl} onChange={(e) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, instagramUrl: e.target.value } : x)))} />
            <ImageField video label="فایل ویدیو (اختیاری)" value={r.videoUrl} onChange={(u) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, videoUrl: u } : x)))} />
          </div>
        ))}
        <Button type="button" variant="outline" onClick={() => set("reels", [...v.reels, { title: "", instagramUrl: "", videoUrl: "", posterUrl: "" }])}><Plus /> افزودن ویدیو</Button>
      </div>
      <div className="space-y-2">
        <p className="text-sm font-semibold">نکته‌های طلایی</p>
        {v.tips.map((t: any, i: number) => (
          <div key={i} className="grid gap-2 rounded-xl border p-3 sm:grid-cols-[1fr_2fr_auto]">
            <Input placeholder="عنوان" value={t.title} onChange={(e) => set("tips", v.tips.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
            <Input placeholder="توضیح" value={t.text} onChange={(e) => set("tips", v.tips.map((x: any, k: number) => (k === i ? { ...x, text: e.target.value } : x)))} />
            <Button type="button" variant="ghost" onClick={() => set("tips", v.tips.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
          </div>
        ))}
        <Button type="button" variant="outline" onClick={() => set("tips", [...v.tips, { title: "", text: "" }])}><Plus /> افزودن نکته</Button>
      </div>
    </>,
    installments: () => <>
      <Toggle label="خرید اقساطی فعال باشد" value={v.enabled} onChange={(x) => set("enabled", x)} />
      <TextField label="عنوان" value={v.title} onChange={(x) => set("title", x)} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="سقف خرید (تومان)" ltr value={String(Math.round(v.maxRial / 10))} onChange={(x) => set("maxRial", (Number(x.replace(/\D/g, "")) || 0) * 10)} />
        <TextField label="پیش‌پرداخت (درصد)" ltr value={String(v.downPercent)} onChange={(x) => set("downPercent", Number(x.replace(/\D/g, "")) || 0)} />
        <TextField label="تعداد قسط (ماه)" ltr value={String(v.months)} onChange={(x) => set("months", Number(x.replace(/\D/g, "")) || 1)} />
        <TextField label="کارمزد ماهانه (درصد، ۰ = بدون سود)" ltr value={String(v.monthlyFeePercent)} onChange={(x) => set("monthlyFeePercent", Number(x.replace(/\D/g, "")) || 0)} />
      </div>
      <TextField label="توضیح" multiline value={v.note} onChange={(x) => set("note", x)} />
    </>,
    publish: () => <>
      <Toggle label="سایت برای همه منتشر شود" value={v.published} onChange={(x) => set("published", x)} />
      <TextField label="پیام «به‌زودی» (وقتی منتشر نشده)" value={v.maintenanceMessage} onChange={(x) => set("maintenanceMessage", x)} />
    </>,
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div><h1 className="text-2xl font-extrabold">{TITLES[section][0]}</h1><p className="mt-1 text-sm text-muted-foreground">{TITLES[section][1]}</p></div>
        <Link href="/" target="_blank" className="flex items-center gap-1 text-sm text-primary">دیدن سایت <ExternalLink className="size-4" /></Link>
      </div>
      <form className="space-y-5 rounded-[var(--radius)] border bg-card p-5" onSubmit={(e) => { e.preventDefault(); save(); }}>
        {body[section]()}
        <div className="sticky bottom-3 flex items-center gap-3">
          <Button type="submit" disabled={saving} data-testid="save">{saving ? "در حال ذخیره…" : "ذخیره"}</Button>
          {msg && <span className={`text-sm ${msg.ok ? "text-emerald-700" : "text-destructive"}`} data-testid="save-msg">{msg.text}</span>}
        </div>
      </form>
    </div>
  );
}
