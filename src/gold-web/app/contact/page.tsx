"use client";

import { useSite } from "@/components/site-provider";

export default function Contact() {
  const { site } = useSite();
  const c = site.contact;
  const rows = [["آدرس", c.address], ["شهر", c.city], ["ساعات کاری", c.workingHours], ["تلفن", c.phone], ["موبایل", c.mobile], ["واتساپ", c.whatsApp], ["اینستاگرام", c.instagram && `@${c.instagram}`], ["تلگرام", c.telegram], ["ایمیل", c.email]].filter(([, v]) => v);
  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-10">
      <h1 className="text-3xl font-extrabold">تماس با {site.brand.name}</h1>
      <dl className="divide-y rounded-[var(--radius)] border bg-card">
        {rows.map(([k, v]) => <div key={k} className="flex justify-between gap-4 p-4"><dt className="text-muted-foreground">{k}</dt><dd className="text-left">{v}</dd></div>)}
      </dl>
      {site.legal.aboutText && <div className="rounded-[var(--radius)] border bg-card p-5 leading-8"><h2 className="mb-2 font-bold">درباره‌ی ما</h2>{site.legal.aboutText}</div>}
      {c.mapUrl && <a href={c.mapUrl} target="_blank" rel="noopener noreferrer" className="inline-block rounded-full border px-5 py-2 hover:bg-accent">مسیریابی روی نقشه</a>}
    </div>
  );
}
