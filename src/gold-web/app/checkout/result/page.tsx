"use client";

import { Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { CheckCircle2, XCircle } from "lucide-react";

function Inner() {
  const sp = useSearchParams();
  const ok = sp.get("status") === "Succeeded";
  return (
    <div className="mx-auto max-w-md space-y-4 px-4 py-24 text-center" data-testid="payment-result">
      {ok ? <CheckCircle2 className="mx-auto size-16 text-emerald-700" /> : <XCircle className="mx-auto size-16 text-destructive" />}
      <h1 className="text-2xl font-extrabold">{ok ? "Zahlung erfolgreich" : "Zahlung fehlgeschlagen"}</h1>
      <p className="text-muted-foreground">{ok ? "Ihre Bestellung ist eingegangen. Sie erhalten eine Bestätigung per SMS." : "Es wurde kein Betrag abgebucht oder er wird innerhalb von 72 Stunden erstattet. Die Stücke sind wieder freigegeben."}</p>
      <Link href="/account" className="inline-block rounded-full border px-5 py-2 hover:bg-accent">Meine Bestellungen</Link>
    </div>
  );
}
export default function Page() { return <Suspense><Inner /></Suspense>; }
