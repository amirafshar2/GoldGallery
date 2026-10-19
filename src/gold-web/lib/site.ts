export type HomeBlock = { key: string; enabled: boolean; order: number; title: string; subtitle: string };
export type Site = {
  brand: { name: string; slogan: string; logoUrl: string; faviconUrl: string };
  theme: { mode: string; background: string; surface: string; text: string; primary: string; muted: string; font: string; radius: string };
  contact: { phone: string; mobile: string; whatsApp: string; instagram: string; telegram: string; email: string; address: string; city: string; workingHours: string; mapUrl: string };
  legal: { licenseNumber: string; economicCode: string; trustSealUrl: string; aboutText: string };
  home: { hero: { title: string; subtitle: string; ctaText: string; ctaLink: string; images: string[] }; blocks: HomeBlock[] };
  trust: { badges: { icon: string; title: string; text: string }[] };
  footer: { text: string; links: { title: string; url: string }[] };
  seo: { title: string; description: string; ogImageUrl: string };
  features: { shop: boolean; nameNecklace: boolean; customOrder: boolean; buyBack: boolean; bridal: boolean; wholesale: boolean; installments: boolean; blog: boolean; repairs: boolean };
  texts: { values: Record<string, string> };
  publish: { published: boolean; maintenanceMessage: string };
  owner: {
    enabled: boolean; name: string; role: string; photoUrl: string; quote: string; highlights: string[];
    instagram: string; followers: string; ctaText: string;
    reels: { title: string; instagramUrl: string; videoUrl: string; posterUrl: string }[];
    tips: { title: string; text: string }[];
  };
  installments: { enabled: boolean; maxRial: number; downPercent: number; months: number; monthlyFeePercent: number; title: string; note: string };
  /** Public demo for presenting the site to a shop (ribbon + noindex + sandbox payments). */
  showcase: boolean;
};

const API = process.env.API_URL ?? "http://localhost:5080";

/** Server-side fetch of the white-label site content (no cache: admin edits must show immediately). */
export async function getSite(): Promise<Site | null> {
  try {
    const r = await fetch(`${API}/api/v1/site`, { cache: "no-store" });
    return r.ok ? ((await r.json()) as Site) : null;
  } catch {
    return null;
  }
}

export async function serverApi<T>(path: string): Promise<T | null> {
  try {
    const r = await fetch(`${API}/api/v1${path}`, { cache: "no-store" });
    return r.ok ? ((await r.json()) as T) : null;
  } catch {
    return null;
  }
}
