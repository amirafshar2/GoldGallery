"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/** First-run of an EMPTY install: the shop owner creates the first admin with the install code. */
export default function Setup() {
  const router = useRouter();
  const [needs, setNeeds] = useState<boolean | null>(null);
  const [f, setF] = useState({ setupToken: "", phone: "", fullName: "" });
  const [err, setErr] = useState("");
  useEffect(() => { api<{ needsSetup: boolean }>("/setup/status").then((s) => setNeeds(s.needsSetup)).catch(() => setNeeds(false)); }, []);
  const submit = async () => {
    setErr("");
    try { await api("/setup/init", { method: "POST", json: f }); router.push("/login?next=/admin"); } catch (e) { setErr((e as ApiError).message); }
  };
  if (needs === null) return null;
  if (!needs) return <p className="py-24 text-center text-muted-foreground">Diese Website wurde bereits eingerichtet.</p>;
  return (
    <div className="mx-auto max-w-md space-y-5 px-4 py-16">
      <h1 className="text-2xl font-extrabold">Willkommen auf Ihrer neuen Website 👋</h1>
      <p className="text-muted-foreground">Den Installationscode haben Sie von Ihrem Webdesigner bekommen. Damit legen Sie Ihr eigenes Admin-Konto an. Nach der Anmeldung führt Sie ein Assistent Schritt für Schritt durch die Einrichtung.</p>
      <div className="space-y-4 rounded-[var(--radius)] border bg-card p-6">
        <div className="space-y-2"><Label>Installationscode</Label><Input dir="ltr" value={f.setupToken} onChange={(e) => setF({ ...f, setupToken: e.target.value })} /></div>
        <div className="space-y-2"><Label>Ihr Name</Label><Input value={f.fullName} onChange={(e) => setF({ ...f, fullName: e.target.value })} /></div>
        <div className="space-y-2"><Label>Handynummer</Label><Input dir="ltr" inputMode="tel" value={f.phone} onChange={(e) => setF({ ...f, phone: e.target.value })} /></div>
        {err && <p className="text-sm text-destructive">{err}</p>}
        <Button className="w-full" onClick={submit}>Admin-Konto anlegen</Button>
      </div>
    </div>
  );
}
