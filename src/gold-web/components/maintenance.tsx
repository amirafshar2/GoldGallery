"use client";

import { usePathname } from "next/navigation";
import { useSite, useMe } from "./site-provider";

/** Unpublished site: visitors see the maintenance message; admin, setup and login pages keep working. */
export function Maintenance({ children }: { children: React.ReactNode }) {
  const { site } = useSite();
  const me = useMe();
  const path = usePathname();
  const open = site.publish.published || me?.role === "Admin" || me?.role === "Staff" ||
    ["/admin", "/setup", "/login", "/checkout"].some((p) => path.startsWith(p));
  if (open) return <>{children}</>;
  return (
    <div className="grid min-h-screen place-items-center p-6 text-center">
      <div className="space-y-4">
        {site.brand.logoUrl && <img src={site.brand.logoUrl} alt="" className="mx-auto h-16 w-auto" />}
        <h1 className="text-2xl font-extrabold gold-text">{site.brand.name || "به‌زودی"}</h1>
        <p className="text-muted-foreground">{site.publish.maintenanceMessage}</p>
        <a href="/login" className="text-sm underline text-muted-foreground">ورود مدیر</a>
      </div>
    </div>
  );
}
