"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { Gem, Wallet, CalendarCheck, ShieldCheck } from "lucide-react";
import { useSite } from "@/components/site-provider";
import { InstallmentCalculator } from "@/components/installments";
import { BlurFade } from "@/components/ui21/blur-fade";
import { num, euro } from "@/lib/format";

function Inner() {
  const { site } = useSite();
  const sp = useSearchParams();
  const t = site.installments;
  const amount = Number(sp.get("amount")) || undefined;
  if (!t.enabled) return <p className="py-24 text-center text-muted-foreground">Der Ratenkauf ist derzeit nicht aktiv.</p>;
  const steps = [
    { icon: Gem, title: "Wunschstück aussuchen", text: `In der Vitrine auf der Website oder im Geschäft – mit einem beliebigen Betrag bis ${euro(t.maxRial)} €.` },
    { icon: Wallet, title: `${num(t.downPercent)} % sofort zahlen`, text: "Ihr Kauf wird heute zum heutigen Preis abgeschlossen." },
    { icon: CalendarCheck, title: `Den Rest in ${num(t.months)} Monatsraten`, text: t.monthlyFeePercent === 0 ? "Ganz ohne Zinsen; steigt der Goldpreis, bleiben Ihre Raten gleich." : `Mit einer monatlichen Gebühr von ${num(t.monthlyFeePercent)} %.` },
  ];
  return (
    <div className="mx-auto max-w-6xl space-y-16 px-4 py-12 sm:py-16">
      <BlurFade className="max-w-3xl">
        <p className="eyebrow">Gold auf Raten · {site.brand.name}</p>
        <h1 className="mt-3 text-4xl font-light leading-[1.4] sm:text-6xl">{t.title}</h1>
        {t.note && <p className="mt-5 text-lg leading-9 text-muted-foreground">{t.note}</p>}
      </BlurFade>
      <InstallmentCalculator initialRial={amount} />
      <div className="grid gap-4 sm:grid-cols-3">
        {steps.map((s, i) => (
          <BlurFade key={s.title} inView delay={i * 0.08} className="rounded-[var(--radius)] border bg-card p-6">
            <div className="flex items-center gap-3"><span className="num grid size-9 place-items-center rounded-full bg-primary text-sm text-primary-foreground">{num(i + 1)}</span><s.icon className="size-5 text-primary" /></div>
            <p className="mt-4 text-lg font-semibold">{s.title}</p>
            <p className="mt-2 leading-7 text-muted-foreground">{s.text}</p>
          </BlurFade>
        ))}
      </div>
      <BlurFade inView className="flex items-start gap-4 rounded-[var(--radius)] border bg-accent/50 p-6">
        <ShieldCheck className="mt-1 size-6 shrink-0 text-primary" />
        <p className="leading-8">Ein Beispiel: Wer vor einem Jahr auf Raten gekauft hat, zahlt heute noch die Raten zum damaligen Preis – auch wenn Gold inzwischen teurer ist. Der Preis wird am Kauftag festgeschrieben.</p>
      </BlurFade>
    </div>
  );
}

export default function InstallmentsPage() {
  return <Suspense><Inner /></Suspense>;
}
