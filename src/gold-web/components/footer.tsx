"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { useSite } from "./site-provider";

export function Footer() {
  const { site, t } = useSite();
  const c = site.contact;
  // Demo-Installation (Demo-Login aktiv): die echte Anbieterkennzeichnung liegt auf der Portfolio-Website.
  const [demo, setDemo] = useState(false);
  useEffect(() => {
    fetch("/api/v1/auth/demo-roles").then((r) => (r.ok ? r.json() : [])).then((x: string[]) => setDemo(x.length > 0)).catch(() => {});
  }, []);
  return (
    <footer className="velvet mt-24">
      <div className="mx-auto grid max-w-7xl gap-8 px-4 py-12 sm:grid-cols-2 lg:grid-cols-4">
        <div className="space-y-3">
          <p className="text-2xl font-extrabold gold-text">{site.brand.name}</p>
          <p className="text-sm text-muted-foreground">{site.brand.slogan}</p>
        </div>
        <div className="space-y-2 text-sm text-muted-foreground">
          {c.address && <p>{c.address}</p>}
          {c.workingHours && <p>{c.workingHours}</p>}
          {c.phone && <p className="num">{c.phone}</p>}
          {c.instagram && <a className="block hover:text-velvet-foreground" href={`https://instagram.com/${c.instagram}`} rel="noopener noreferrer" target="_blank">@{c.instagram}</a>}
        </div>
        <div className="grid content-start gap-2 text-sm">
          {site.footer.links.map((l) => <Link key={l.url + l.title} href={l.url} className="text-muted-foreground hover:text-velvet-foreground">{l.title}</Link>)}
        </div>
        <div className="space-y-2 text-xs text-muted-foreground">
          {site.legal.licenseNumber && <p>Handelsregister: <span className="num">{site.legal.licenseNumber}</span></p>}
          {site.legal.economicCode && <p>USt-IdNr.: <span className="num">{site.legal.economicCode}</span></p>}
          {site.legal.trustSealUrl && <img src={site.legal.trustSealUrl} alt="Gütesiegel" className="h-16 w-auto" />}
        </div>
      </div>
      <div className="border-t px-4 py-4 text-center text-xs text-muted-foreground">
        {site.footer.text} {t("footer.rights")}
        {demo && (
          <p className="mt-2" data-testid="demo-legal">
            Demo-Projekt – fiktives Unternehmen, es werden keine persönlichen Daten erhoben ·{" "}
            <a href="https://amirrezaafshar.de/impressum" target="_blank" rel="noopener noreferrer" className="underline-offset-4 hover:underline">Impressum</a> ·{" "}
            <a href="https://amirrezaafshar.de/datenschutz" target="_blank" rel="noopener noreferrer" className="underline-offset-4 hover:underline">Datenschutz</a>
          </p>
        )}
      </div>
    </footer>
  );
}
