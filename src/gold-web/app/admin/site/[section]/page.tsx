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
  brand: ["Name und Logo", "Diese Angaben erscheinen oben auf jeder Seite und im Browser-Tab."],
  theme: ["Farben und Aussehen", "Farben gelten sofort für die ganze Website. Nehmen Sie als „Hauptfarbe“ das Gold aus Ihrem Logo."],
  contact: ["Kontakt und Adresse", "Wird im Footer, auf der Kontaktseite und im Bereich „Vitrine“ der Startseite angezeigt."],
  legal: ["Rechtliches und Über uns", "Handelsregister und USt-IdNr. schaffen Vertrauen bei Kunden."],
  home: ["Startseite", "Überschrift und Scroll-Bilder oben auf der Seite + Reihenfolge und Ein/Aus der Bereiche."],
  trust: ["Vorteile (Warum wir?)", "Kleine Vertrauenskarten wie „Ordentliche Rechnung“ und „14 Tage Umtausch“."],
  footer: ["Footer", "Text und Links unten auf der Seite."],
  seo: ["SEO", "Titel und Beschreibung, die bei Google und beim Teilen eines Links erscheinen."],
  features: ["Aktive Bereiche", "Schalten Sie jeden Bereich aus, den Sie nicht brauchen; er verschwindet aus Menü und Startseite."],
  texts: ["Alle Texte der Website", "Jede Beschriftung lässt sich ändern. Leer lassen, um den Standardtext zu zeigen."],
  publish: ["Website veröffentlichen", "Solange die Website nicht veröffentlicht ist, sehen Besucher nur die Meldung „Bald für Sie da“."],
  owner: ["Inhaber / Berater (Gesicht der Marke)", "Wenn der Inhaber selbst vor der Kamera steht, kommen hier Foto, Zitat, Videos und Tipps hin."],
  installments: ["Ratenkauf", "Die Ratenkonditionen gelten für den Rechner auf der Startseite und die Seite „Ratenkauf“."],
};
const ICONS = ["receipt", "refresh", "wrench", "truck", "shield", "sparkles"];
const BLOCK_NAMES: Record<string, string> = { "rate-ticker": "Live-Goldkurs", "new-arrivals": "Neuheiten", categories: "Kategorien", gallery: "Scroll-Galerie (Auswahl)", owner: "Inhaber / Berater", installments: "Ratenrechner", story: "Markengeschichte (Bild + Text)", "custom-order": "Anfertigung nach Wunsch", "buy-back": "Altgold-Ankauf", trust: "Vorteile", testimonials: "Kundenstimmen", contact: "Geschäft und Anfahrt" };
const FEATURES: [string, string][] = [["shop", "Onlineshop"], ["nameNecklace", "Namensketten"], ["customOrder", "Anfertigung nach Wunsch"], ["buyBack", "Altgold-Ankauf"], ["bridal", "Braut-Sets"], ["wholesale", "Großhandelsportal"], ["installments", "Ratenkauf"], ["blog", "Blog"], ["repairs", "Reparaturen"]];

/* eslint-disable @typescript-eslint/no-explicit-any */
export default function SectionEditor({ params }: { params: Promise<{ section: string }> }) {
  const { section } = use(params);
  const [v, setV] = useState<any>(null);
  const [msg, setMsg] = useState<{ ok: boolean; text: string } | null>(null);
  const [saving, setSaving] = useState(false);
  const router = useRouter();

  useEffect(() => { setV(null); api<Site>("/site").then((s) => setV(structuredClone((s as any)[section]))); }, [section]);
  if (!TITLES[section]) return <p>Unbekannter Bereich.</p>;
  if (!v) return <p className="text-muted-foreground">Wird geladen…</p>;

  const set = (k: string, val: unknown) => setV({ ...v, [k]: val });
  const save = async () => {
    setSaving(true); setMsg(null);
    try { await api(`/admin/site/${section}`, { method: "PUT", json: v }); setMsg({ ok: true, text: "Gespeichert ✓" }); window.dispatchEvent(new Event("site-saved")); router.refresh(); }
    catch (e) { setMsg({ ok: false, text: (e as ApiError).message }); } finally { setSaving(false); }
  };

  const body: Record<string, () => React.ReactNode> = {
    brand: () => <>
      <TextField label="Name des Geschäfts" value={v.name} onChange={(x) => set("name", x)} placeholder="z. B. Goldatelier Sonne" />
      <TextField label="Slogan" value={v.slogan} onChange={(x) => set("slogan", x)} />
      <ImageField label="Logo" hint="Am besten ein PNG mit transparentem Hintergrund." value={v.logoUrl} onChange={(x) => set("logoUrl", x)} />
      <ImageField label="Browser-Tab-Symbol (optional)" value={v.faviconUrl} onChange={(x) => set("faviconUrl", x)} />
    </>,
    theme: () => <div className="grid gap-4 sm:grid-cols-2">
      <ColorField label="Hauptfarbe (Gold der Marke)" value={v.primary} onChange={(x) => set("primary", x)} />
      <ColorField label="Hintergrund" value={v.background} onChange={(x) => set("background", x)} />
      <ColorField label="Karten" value={v.surface} onChange={(x) => set("surface", x)} />
      <ColorField label="Text" value={v.text} onChange={(x) => set("text", x)} />
      <ColorField label="Text (hell)" value={v.muted} onChange={(x) => set("muted", x)} />
      <TextField label="Eckenradius" value={v.radius} ltr onChange={(x) => set("radius", x)} hint="z. B. 16px" />
      <div className="flex gap-2 sm:col-span-2">
        <Button type="button" variant="outline" onClick={() => setV({ ...v, mode: "dark", background: "#0B0A08", surface: "#15130F", text: "#F4EFE6", primary: "#C9A24A", muted: "#B8AD99" })}>Dunkles Samt-Design</Button>
        <Button type="button" variant="outline" onClick={() => setV({ ...v, mode: "light", background: "#F6F1E9", surface: "#FFFDF9", text: "#1D1812", primary: "#8E6B2F", muted: "#6F6556" })}>Elfenbein und Samt (Standard)</Button>
      </div>
    </div>,
    contact: () => <div className="grid gap-4 sm:grid-cols-2">
      {([["phone", "Festnetz"], ["mobile", "Mobil"], ["whatsApp", "WhatsApp"], ["instagram", "Instagram (ohne @)"], ["telegram", "Telegram"], ["email", "E-Mail"], ["city", "Stadt"], ["workingHours", "Öffnungszeiten"], ["mapUrl", "Kartenlink (Google Maps)"]] as const)
        .map(([k, l]) => <TextField key={k} label={l} value={v[k]} ltr={["phone", "mobile", "whatsApp", "email", "mapUrl", "instagram", "telegram"].includes(k)} onChange={(x) => set(k, x)} />)}
      <div className="sm:col-span-2"><TextField label="Vollständige Adresse" multiline value={v.address} onChange={(x) => set("address", x)} /></div>
    </div>,
    legal: () => <>
      <TextField label="Handelsregisternummer" value={v.licenseNumber} onChange={(x) => set("licenseNumber", x)} />
      <TextField label="USt-IdNr." value={v.economicCode} onChange={(x) => set("economicCode", x)} />
      <ImageField label="Gütesiegel-Bild (optional)" value={v.trustSealUrl} onChange={(x) => set("trustSealUrl", x)} />
      <TextField label="Über uns" multiline value={v.aboutText} onChange={(x) => set("aboutText", x)} />
    </>,
    home: () => <>
      <TextField label="Große Überschrift oben" value={v.hero.title} onChange={(x) => set("hero", { ...v.hero, title: x })} />
      <TextField label="Unterzeile" value={v.hero.subtitle} onChange={(x) => set("hero", { ...v.hero, subtitle: x })} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Button-Text" value={v.hero.ctaText} onChange={(x) => set("hero", { ...v.hero, ctaText: x })} />
        <TextField label="Button-Link" ltr value={v.hero.ctaLink} onChange={(x) => set("hero", { ...v.hero, ctaLink: x })} />
      </div>
      <ImageList label="Scroll-Bilder (bis zu 7)" hint="Das erste Bild steht in der Mitte und wird beim Scrollen bildschirmfüllend; die anderen zoomen drumherum. Makro- und dunkle Bilder wirken am besten." value={v.hero.images ?? []} onChange={(x) => set("hero", { ...v.hero, images: x })} />
      <div className="space-y-2">
        <p className="text-sm font-semibold">Bereiche der Startseite (Reihenfolge und Ein/Aus)</p>
        {v.blocks.map((b: any, i: number) => (
          <div key={b.key} className="flex flex-wrap items-center gap-2 rounded-xl border p-2">
            <div className="flex flex-col">
              <button type="button" onClick={() => i > 0 && set("blocks", swap(v.blocks, i, i - 1).map((x: any, k: number) => ({ ...x, order: k + 1 })))} aria-label="nach oben"><ArrowUp className="size-4" /></button>
              <button type="button" onClick={() => i < v.blocks.length - 1 && set("blocks", swap(v.blocks, i, i + 1).map((x: any, k: number) => ({ ...x, order: k + 1 })))} aria-label="nach unten"><ArrowDown className="size-4" /></button>
            </div>
            <span className="w-32 text-xs text-muted-foreground">{BLOCK_NAMES[b.key] ?? b.key}</span>
            <Input className="min-w-40 flex-1" value={b.title} onChange={(e) => set("blocks", v.blocks.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
            <button type="button" onClick={() => set("blocks", v.blocks.map((x: any, k: number) => (k === i ? { ...x, enabled: !x.enabled } : x)))}
              className={`rounded-full px-3 py-1 text-xs ${b.enabled ? "bg-primary text-primary-foreground" : "bg-muted"}`}>{b.enabled ? "an" : "aus"}</button>
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
          <Input placeholder="Titel" value={b.title} onChange={(e) => set("badges", v.badges.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
          <Input placeholder="Beschreibung" value={b.text} onChange={(e) => set("badges", v.badges.map((x: any, k: number) => (k === i ? { ...x, text: e.target.value } : x)))} />
          <Button type="button" variant="ghost" onClick={() => set("badges", v.badges.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
        </div>
      ))}
      <Button type="button" variant="outline" onClick={() => set("badges", [...v.badges, { icon: "shield", title: "", text: "" }])}><Plus /> Vorteil hinzufügen</Button>
    </>,
    footer: () => <>
      <TextField label="Footer-Text" multiline value={v.text} onChange={(x) => set("text", x)} />
      {v.links.map((l: any, i: number) => (
        <div key={i} className="flex gap-2">
          <Input placeholder="Titel" value={l.title} onChange={(e) => set("links", v.links.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
          <Input placeholder="/terms" dir="ltr" value={l.url} onChange={(e) => set("links", v.links.map((x: any, k: number) => (k === i ? { ...x, url: e.target.value } : x)))} />
          <Button type="button" variant="ghost" onClick={() => set("links", v.links.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
        </div>
      ))}
      <Button type="button" variant="outline" onClick={() => set("links", [...v.links, { title: "", url: "/" }])}><Plus /> Link hinzufügen</Button>
    </>,
    seo: () => <>
      <TextField label="Titel bei Google" value={v.title} onChange={(x) => set("title", x)} />
      <TextField label="Beschreibung bei Google" multiline value={v.description} onChange={(x) => set("description", x)} />
      <ImageField label="Vorschaubild beim Teilen" value={v.ogImageUrl} onChange={(x) => set("ogImageUrl", x)} />
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
      <Toggle label="Diesen Bereich auf der Startseite anzeigen" value={v.enabled} onChange={(x) => set("enabled", x)} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Name" value={v.name} onChange={(x) => set("name", x)} placeholder="z. B. Markus Lindner" />
        <TextField label="Position" value={v.role} onChange={(x) => set("role", x)} />
        <TextField label="Persönliches Instagram (ohne @)" ltr value={v.instagram} onChange={(x) => set("instagram", x)} />
        <TextField label="Anzahl Follower (optional)" value={v.followers} onChange={(x) => set("followers", x)} />
      </div>
      <ImageField label="Foto (hochkant, Porträt)" hint="Am besten: Hochformat mit dunklem Hintergrund und warmem Licht." value={v.photoUrl} onChange={(x) => set("photoUrl", x)} />
      <TextField label="Leitsatz" multiline value={v.quote} onChange={(x) => set("quote", x)} />
      <TextField label="Text des Beratungs-Buttons" value={v.ctaText} onChange={(x) => set("ctaText", x)} />
      <div className="space-y-2">
        <p className="text-sm font-semibold">Stärken (eine pro Zeile)</p>
        <textarea className="min-h-24 w-full rounded-md border bg-background p-2 text-sm" value={(v.highlights ?? []).join("\n")} onChange={(e) => set("highlights", e.target.value.split("\n").filter((x) => x.trim()))} />
      </div>
      <div className="space-y-2">
        <p className="text-sm font-semibold">Videos</p>
        <p className="text-xs text-muted-foreground">Link zu einem Instagram-Reel eintragen oder eine MP4-Datei (bis 25 MB) hochladen.</p>
        {v.reels.map((r: any, i: number) => (
          <div key={i} className="grid gap-2 rounded-xl border p-3">
            <div className="flex gap-2">
              <Input placeholder="Videotitel" value={r.title} onChange={(e) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
              <Button type="button" variant="ghost" onClick={() => set("reels", v.reels.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
            </div>
            <Input placeholder="https://www.instagram.com/reel/…" dir="ltr" value={r.instagramUrl} onChange={(e) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, instagramUrl: e.target.value } : x)))} />
            <ImageField video label="Videodatei (optional)" value={r.videoUrl} onChange={(u) => set("reels", v.reels.map((x: any, k: number) => (k === i ? { ...x, videoUrl: u } : x)))} />
          </div>
        ))}
        <Button type="button" variant="outline" onClick={() => set("reels", [...v.reels, { title: "", instagramUrl: "", videoUrl: "", posterUrl: "" }])}><Plus /> Video hinzufügen</Button>
      </div>
      <div className="space-y-2">
        <p className="text-sm font-semibold">Goldene Tipps</p>
        {v.tips.map((t: any, i: number) => (
          <div key={i} className="grid gap-2 rounded-xl border p-3 sm:grid-cols-[1fr_2fr_auto]">
            <Input placeholder="Titel" value={t.title} onChange={(e) => set("tips", v.tips.map((x: any, k: number) => (k === i ? { ...x, title: e.target.value } : x)))} />
            <Input placeholder="Beschreibung" value={t.text} onChange={(e) => set("tips", v.tips.map((x: any, k: number) => (k === i ? { ...x, text: e.target.value } : x)))} />
            <Button type="button" variant="ghost" onClick={() => set("tips", v.tips.filter((_: any, k: number) => k !== i))}><Trash2 /></Button>
          </div>
        ))}
        <Button type="button" variant="outline" onClick={() => set("tips", [...v.tips, { title: "", text: "" }])}><Plus /> Tipp hinzufügen</Button>
      </div>
    </>,
    installments: () => <>
      <Toggle label="Ratenkauf aktivieren" value={v.enabled} onChange={(x) => set("enabled", x)} />
      <TextField label="Titel" value={v.title} onChange={(x) => set("title", x)} />
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField label="Höchstbetrag (€)" ltr value={String(Math.round(v.maxRial / 100))} onChange={(x) => set("maxRial", (Number(x.replace(/\D/g, "")) || 0) * 100)} />
        <TextField label="Anzahlung (Prozent)" ltr value={String(v.downPercent)} onChange={(x) => set("downPercent", Number(x.replace(/\D/g, "")) || 0)} />
        <TextField label="Anzahl Raten (Monate)" ltr value={String(v.months)} onChange={(x) => set("months", Number(x.replace(/\D/g, "")) || 1)} />
        <TextField label="Monatliche Gebühr (Prozent, 0 = zinsfrei)" ltr value={String(v.monthlyFeePercent)} onChange={(x) => set("monthlyFeePercent", Number(x.replace(/\D/g, "")) || 0)} />
      </div>
      <TextField label="Beschreibung" multiline value={v.note} onChange={(x) => set("note", x)} />
    </>,
    publish: () => <>
      <Toggle label="Website für alle veröffentlichen" value={v.published} onChange={(x) => set("published", x)} />
      <TextField label="Meldung „Bald für Sie da“ (solange nicht veröffentlicht)" value={v.maintenanceMessage} onChange={(x) => set("maintenanceMessage", x)} />
    </>,
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div><h1 className="text-2xl font-extrabold">{TITLES[section][0]}</h1><p className="mt-1 text-sm text-muted-foreground">{TITLES[section][1]}</p></div>
        <Link href="/" target="_blank" className="flex items-center gap-1 text-sm text-primary">Website ansehen <ExternalLink className="size-4" /></Link>
      </div>
      <form className="space-y-5 rounded-[var(--radius)] border bg-card p-5" onSubmit={(e) => { e.preventDefault(); save(); }}>
        {body[section]()}
        <div className="sticky bottom-3 flex items-center gap-3">
          <Button type="submit" disabled={saving} data-testid="save">{saving ? "Wird gespeichert…" : "Speichern"}</Button>
          {msg && <span className={`text-sm ${msg.ok ? "text-emerald-700" : "text-destructive"}`} data-testid="save-msg">{msg.text}</span>}
        </div>
      </form>
    </div>
  );
}
