"use client";

/** Installment calculator — terms come from admin → «خرید اقساطی» (down %, months, cap, fee). Money stays integer rial. */
import Link from "next/link";
import { useState } from "react";
import { ArrowLeft, CalendarCheck, MessageCircle, Wallet } from "lucide-react";
import { useSite } from "./site-provider";
import { BlurFade } from "./ui21/blur-fade";
import { BorderBeam } from "./ui21/border-beam";
import { toman, faNum } from "@/lib/format";
import { waLink } from "@/lib/utils";

export function plan(amountRial: number, downPercent: number, months: number, feePercent: number) {
  const down = Math.round((amountRial * downPercent) / 100);
  const rest = amountRial - down;
  const total = rest + Math.round((rest * feePercent * months) / 100);
  const monthly = Math.ceil(total / Math.max(1, months));
  return { down, rest, monthly, total: down + monthly * months };
}

export function InstallmentCalculator({ initialRial, compact = false }: { initialRial?: number; compact?: boolean }) {
  const { site } = useSite();
  const t = site.installments;
  const maxToman = Math.max(10_000_000, Math.round(t.maxRial / 10));
  const start = Math.min(maxToman, Math.max(10_000_000, Math.round((initialRial ?? 1_000_000_000) / 10)));
  const [amountToman, setAmount] = useState(start);
  const p = plan(amountToman * 10, t.downPercent, t.months, t.monthlyFeePercent);
  const wa = site.contact.whatsApp || site.contact.mobile;
  const msg = `سلام، برای خرید اقساطی طلا به مبلغ ${toman(amountToman * 10)} تومان (پیش‌پرداخت ${toman(p.down)} و ${faNum(t.months)} قسط ${toman(p.monthly)} تومانی) راهنمایی می‌خواهم.`;

  return (
    <div className="relative overflow-hidden rounded-[calc(var(--radius)*1.25)] border bg-card p-6 shadow-[0_30px_80px_-40px_rgba(60,40,10,.35)] sm:p-10" data-testid="installment-calc">
      <div className="grid gap-10 lg:grid-cols-2">
        <div className="space-y-6">
          <div>
            <p className="text-sm text-muted-foreground">مبلغ خرید طلا</p>
            <p className="mt-1 text-4xl font-extrabold text-primary sm:text-5xl"><span className="num">{toman(amountToman * 10)}</span> <span className="text-base font-normal text-muted-foreground">تومان</span></p>
          </div>
          <input type="range" min={10_000_000} max={maxToman} step={5_000_000} value={amountToman}
            onChange={(e) => setAmount(Number(e.target.value))} aria-label="مبلغ خرید"
            className="w-full accent-[var(--brand-primary)]" data-testid="installment-range" />
          <div className="flex justify-between text-xs text-muted-foreground"><span>۱۰ میلیون</span><span>تا سقف {toman(maxToman * 10)} تومان</span></div>
          {!compact && t.note && <p className="leading-8 text-muted-foreground">{t.note}</p>}
        </div>

        <div className="space-y-5">
          <div className="grid grid-cols-2 gap-3">
            <Stat icon={<Wallet className="size-5" />} label={`پیش‌پرداخت (${faNum(t.downPercent)}٪)`} value={toman(p.down)} />
            <Stat icon={<CalendarCheck className="size-5" />} label={`هر قسط ماهانه`} value={toman(p.monthly)} />
          </div>
          <div>
            <p className="mb-3 text-sm text-muted-foreground">برنامه‌ی پرداخت {t.monthlyFeePercent === 0 && <b className="text-emerald-700">— بدون سود</b>}</p>
            <ol className="flex items-center gap-1">
              <li className="flex-1"><div className="h-2 rounded-full bg-primary" /><p className="mt-2 text-center text-[11px]">امروز</p></li>
              {Array.from({ length: t.months }).map((_, i) => (
                <li key={i} className="flex-1"><div className="h-2 rounded-full bg-primary/30" /><p className="mt-2 text-center text-[11px] text-muted-foreground">ماه {faNum(i + 1)}</p></li>
              ))}
            </ol>
          </div>
          <p className="text-sm text-muted-foreground">جمع پرداختی: <b className="num text-foreground">{toman(p.total)}</b> تومان</p>
          <div className="flex flex-wrap gap-3">
            {wa && (
              <a href={waLink(wa, msg)} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-2 rounded-full bg-[#1f9d55] px-6 py-3 font-semibold text-white hover:brightness-110">
                <MessageCircle className="size-5" /> درخواست خرید اقساطی
              </a>
            )}
            {compact && <Link href="/installments" className="inline-flex items-center gap-1 rounded-full border px-5 py-3 text-sm hover:border-primary hover:text-primary">شرایط کامل <ArrowLeft className="size-4" /></Link>}
          </div>
        </div>
      </div>
      <BorderBeam size={180} duration={14} colorFrom="#F3D58A" colorTo="#8C6A2E" />
    </div>
  );
}

function Stat({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <div className="rounded-[var(--radius)] border bg-background/60 p-4">
      <div className="flex items-center gap-2 text-xs text-muted-foreground"><span className="text-primary">{icon}</span>{label}</div>
      <p className="mt-2 text-xl font-extrabold sm:text-2xl"><span className="num">{value}</span> <span className="text-xs font-normal text-muted-foreground">تومان</span></p>
    </div>
  );
}

export function InstallmentsBlock({ title }: { title: string }) {
  const { site } = useSite();
  if (!site.installments.enabled) return null;
  return (
    <section className="mx-auto max-w-7xl px-4">
      <BlurFade inView className="mb-8 sm:mb-10">
        <p className="eyebrow">طلای اقساطی</p>
        <h2 className="mt-2 text-3xl font-light sm:text-5xl">{title || site.installments.title}</h2>
      </BlurFade>
      <InstallmentCalculator compact />
    </section>
  );
}
