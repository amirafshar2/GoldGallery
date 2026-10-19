"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { CheckCircle2, Circle, Compass, X, ArrowLeft } from "lucide-react";
import { api } from "@/lib/api";
import { faNum } from "@/lib/format";

export type Step = { order: number; key: string; title: string; description: string; adminLink: string; done: boolean };
export type Onboarding = { steps: Step[]; percent: number; next: Step | null; dismissed: boolean; completed: boolean };

export function useOnboarding() {
  const [ob, setOb] = useState<Onboarding | null>(null);
  const reload = useCallback(() => api<Onboarding>("/admin/onboarding").then(setOb).catch(() => {}), []);
  useEffect(() => { reload(); window.addEventListener("site-saved", reload); return () => window.removeEventListener("site-saved", reload); }, [reload]);
  return { ob, reload };
}

/** Step-by-step setup assistant. Opens by itself until the site is complete or the owner hides it. */
export function OnboardingGuide({ inline = false }: { inline?: boolean }) {
  const { ob, reload } = useOnboarding();
  const path = usePathname();
  const [open, setOpen] = useState<boolean | null>(null);
  useEffect(() => {
    // Desktop: side panel opens by itself. Phone: the sheet opens on the dashboard only, so it never hides a form's Save button.
    if (ob && open === null) setOpen(!ob.dismissed && !ob.completed && (inline || path === "/admin"));
  }, [ob, open, inline, path]);
  useEffect(() => { reload(); }, [path, reload]);
  if (!ob) return null;

  const hide = async () => { setOpen(false); await api("/admin/site/onboarding", { method: "PUT", json: { dismissed: true } }).catch(() => {}); };

  if (!open) {
    return ob.completed ? null : (
      <button onClick={() => setOpen(true)} data-testid={inline ? "guide-open" : "guide-open-mobile"} className={inline ? "flex w-full items-center justify-center gap-2 rounded-[var(--radius)] border border-primary bg-card px-4 py-3 text-sm" : "fixed bottom-20 right-4 z-50 flex items-center gap-2 rounded-full border border-primary bg-card px-4 py-3 text-sm shadow-2xl"}>
        <Compass className="size-4 text-primary" /> راهنمای راه‌اندازی <b className="num text-primary">{faNum(ob.percent)}٪</b>
      </button>
    );
  }
  return (
    <aside data-testid={inline ? "onboarding" : "onboarding-mobile"} className={inline
      ? "sticky top-36 max-h-[calc(100vh-10rem)] overflow-y-auto rounded-[var(--radius)] border border-primary/60 bg-card p-4"
      : "fixed inset-x-3 bottom-3 z-50 max-h-[75vh] overflow-y-auto rounded-[var(--radius)] border border-primary/60 bg-card p-4 shadow-2xl"}>
      <div className="mb-3 flex items-start justify-between gap-2">
        <div>
          <p className="flex items-center gap-2 font-bold"><Compass className="size-5 text-primary" /> دستیار راه‌اندازی سایت</p>
          <p className="text-xs text-muted-foreground">قدم‌به‌قدم پر کنید؛ سایت جلوی چشمتان شکل می‌گیرد.</p>
        </div>
        <button onClick={() => setOpen(false)} aria-label="بستن"><X className="size-4" /></button>
      </div>
      <div className="mb-4 h-2 overflow-hidden rounded-full bg-muted"><div className="h-full bg-primary transition-all" style={{ width: `${ob.percent}%` }} /></div>
      {ob.next && (
        <Link href={ob.next.adminLink} data-testid="guide-next" className="mb-4 block rounded-xl bg-primary p-3 text-primary-foreground">
          <span className="text-xs opacity-80">قدم بعدی ({faNum(ob.next.order)} از {faNum(ob.steps.length)})</span>
          <span className="flex items-center justify-between font-bold">{ob.next.title} <ArrowLeft className="size-4" /></span>
          <span className="mt-1 block text-xs opacity-90">{ob.next.description}</span>
        </Link>
      )}
      <ol className="space-y-1">
        {ob.steps.map((s) => (
          <li key={s.key}>
            <Link href={s.adminLink} className={`flex items-center gap-2 rounded-lg px-2 py-2 text-sm hover:bg-accent ${path === s.adminLink ? "bg-accent" : ""}`}>
              {s.done ? <CheckCircle2 className="size-4 shrink-0 text-emerald-700" /> : <Circle className="size-4 shrink-0 text-muted-foreground" />}
              <span className={s.done ? "text-muted-foreground line-through" : ""}>{faNum(s.order)}. {s.title}</span>
            </Link>
          </li>
        ))}
      </ol>
      {ob.completed ? <p className="mt-3 text-center text-sm text-emerald-700">سایت شما آماده است 🎉</p>
        : <button onClick={hide} className="mt-3 w-full text-center text-xs text-muted-foreground underline">بعداً — مخفی کن</button>}
    </aside>
  );
}
