"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { ShoppingBag, Building2, BadgeCheck, Crown } from "lucide-react";
import { api, auth, demoLogin, type Me, ApiError } from "@/lib/api";
import { useSite } from "@/components/site-provider";
import { DEMO_ROLES } from "@/components/role-switcher";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const ICONS = { customer: ShoppingBag, wholesale: Building2, staff: BadgeCheck, admin: Crown } as const;

function Inner() {
  const { site, t } = useSite();
  const router = useRouter();
  const next = useSearchParams().get("next");
  const [phone, setPhone] = useState("");
  const [code, setCode] = useState("");
  const [sent, setSent] = useState(false);
  const [err, setErr] = useState("");
  const [demoRoles, setDemoRoles] = useState<string[]>([]);

  useEffect(() => { fetch("/api/v1/auth/demo-roles").then((r) => (r.ok ? r.json() : [])).then(setDemoRoles).catch(() => {}); }, []);
  const go = (u: Me) => router.push(next ?? (u.role === "Admin" || u.role === "Staff" ? "/admin" : "/account"));

  const send = async () => {
    setErr("");
    try { await api("/auth/otp/request", { method: "POST", json: { phone } }); setSent(true); } catch (e) { setErr((e as ApiError).message); }
  };
  const verify = async () => {
    setErr("");
    try {
      const r = await api<{ accessToken: string; user: Me }>("/auth/otp/verify", { method: "POST", json: { phone, code } });
      auth.set(r.accessToken, r.user); go(r.user);
    } catch (e) { setErr((e as ApiError).message); }
  };

  return (
    <div className="mx-auto max-w-4xl px-4 py-12">
      <div className="mx-auto max-w-sm space-y-4 rounded-[var(--radius)] border bg-card p-6">
        <h1 className="text-center text-2xl font-extrabold">{t("login.title")}</h1>
        <p className="text-center text-sm text-muted-foreground">{site.brand.name}</p>
        <div className="space-y-2">
          <Label htmlFor="phone">{t("login.phone")}</Label>
          <Input id="phone" inputMode="tel" dir="ltr" placeholder="09xxxxxxxxx" value={phone} onChange={(e) => setPhone(e.target.value)} disabled={sent} />
        </div>
        {sent && (
          <div className="space-y-2">
            <Label htmlFor="code">{t("login.code")}</Label>
            <Input id="code" inputMode="numeric" dir="ltr" maxLength={5} value={code} onChange={(e) => setCode(e.target.value)} autoFocus />
          </div>
        )}
        {err && <p className="text-sm text-destructive">{err}</p>}
        <Button className="w-full" onClick={sent ? verify : send}>{sent ? t("login.verify") : t("login.send")}</Button>
      </div>

      {demoRoles.length > 0 && (
        <section className="mt-10" data-testid="demo-login">
          <h2 className="mb-4 text-center text-sm text-muted-foreground">{t("login.demo")}</h2>
          {/* mobile: stacked full width · tablet: 2×2 · desktop: one row of 4 */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            {DEMO_ROLES.filter((r) => demoRoles.includes(r.role)).map((r) => {
              const I = ICONS[r.role];
              return (
                <button key={r.role} data-testid={`demo-${r.role}`} onClick={async () => go(await demoLogin(r.role))}
                  className="flex min-h-12 items-center gap-3 rounded-[var(--radius)] border bg-card p-4 text-right transition-colors hover:border-primary">
                  <I className="size-6 shrink-0 text-primary" />
                  <span><b className="block">{r.label}</b><span className="text-xs text-muted-foreground">{r.hint}</span></span>
                </button>
              );
            })}
          </div>
        </section>
      )}
    </div>
  );
}
export default function LoginPage() { return <Suspense><Inner /></Suspense>; }
