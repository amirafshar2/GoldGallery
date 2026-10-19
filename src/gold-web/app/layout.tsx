import "./globals.css";
import type { Metadata } from "next";
import { getSite, type Site } from "@/lib/site";
import { SiteProvider } from "@/components/site-provider";
import { Header } from "@/components/header";
import { Footer } from "@/components/footer";
import { RoleSwitcher } from "@/components/role-switcher";
import { Maintenance } from "@/components/maintenance";
import { WhatsAppFloat } from "@/components/whatsapp-float";

export async function generateMetadata(): Promise<Metadata> {
  const s = await getSite();
  return {
    title: s?.seo.title || s?.brand.name || "فروشگاه طلا",
    description: s?.seo.description || s?.brand.slogan || "",
    icons: s?.brand.faviconUrl ? [{ url: s.brand.faviconUrl }] : undefined,
    openGraph: s?.seo.ogImageUrl ? { images: [s.seo.ogImageUrl] } : undefined,
    // A public showcase must never be indexed as if it were the shop's real site.
    robots: s?.showcase ? { index: false, follow: false } : undefined,
  };
}

function themeCss(s: Site) {
  const t = s.theme;
  const ok = (c: string) => /^#[0-9a-fA-F]{6}$/.test(c);
  const v = (name: string, val: string) => (ok(val) ? `--brand-${name}:${val};` : "");
  const dark = t.mode === "dark";
  // Velvet bands stay dark in both modes; in dark mode they follow the page background.
  const velvet = dark && ok(t.background) ? `--brand-velvet:${t.background};` : "";
  const onPrimary = dark ? "--brand-on-primary:#14110a;" : "";
  return `:root{${v("bg", t.background)}${v("surface", t.surface)}${v("text", t.text)}${v("primary", t.primary)}${v("muted", t.muted)}${/^\d{1,2}px$/.test(t.radius) ? `--brand-radius:${t.radius};` : ""}${velvet}${onPrimary}}`;
}

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const site = await getSite();
  if (!site) {
    return (
      <html lang="fa" dir="rtl"><body className="grid min-h-screen place-items-center p-6 text-center">
        <p>اتصال به سرور برقرار نشد. لطفاً سرور API را اجرا کنید.</p>
      </body></html>
    );
  }
  return (
    <html lang="fa" dir="rtl">
      <head><style dangerouslySetInnerHTML={{ __html: themeCss(site) }} /></head>
      <body className="min-h-screen">
        <SiteProvider site={site}>
          <Maintenance>
            {site.showcase && (
              <div className="bg-[repeating-linear-gradient(135deg,#1d1812,#1d1812_12px,#2a2219_12px,#2a2219_24px)] px-4 py-1.5 text-center text-[11px] text-[#F3D58A]" data-testid="showcase-ribbon">
                نسخه‌ی نمایشی — طراحی پیشنهادی وب‌سایت {site.brand.name}. پرداخت‌ها آزمایشی‌اند و سفارشی ثبت نمی‌شود.
              </div>
            )}
            <Header />
            <main className="min-h-[60vh]">{children}</main>
            <Footer />
          </Maintenance>
          <RoleSwitcher />
          <WhatsAppFloat />
        </SiteProvider>
      </body>
    </html>
  );
}
