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
      <h1 className="text-2xl font-extrabold">{ok ? "پرداخت موفق بود" : "پرداخت انجام نشد"}</h1>
      <p className="text-muted-foreground">{ok ? "سفارش شما ثبت شد. پیامک تأیید ارسال می‌شود." : "مبلغی کسر نشده یا ظرف ۷۲ ساعت برمی‌گردد. قطعه‌ها دوباره آزاد شدند."}</p>
      <Link href="/account" className="inline-block rounded-full border px-5 py-2 hover:bg-accent">سفارش‌های من</Link>
    </div>
  );
}
export default function Page() { return <Suspense><Inner /></Suspense>; }
