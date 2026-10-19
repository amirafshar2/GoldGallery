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
  if (!needs) return <p className="py-24 text-center text-muted-foreground">این سایت قبلاً راه‌اندازی شده است.</p>;
  return (
    <div className="mx-auto max-w-md space-y-5 px-4 py-16">
      <h1 className="text-2xl font-extrabold">به سایت جدیدتان خوش آمدید 👋</h1>
      <p className="text-muted-foreground">کد نصب را از طراح سایت گرفته‌اید. با آن، حساب مدیر خودتان را بسازید. بعد از ورود، دستیار قدم‌به‌قدم کمکتان می‌کند سایت را پر کنید.</p>
      <div className="space-y-4 rounded-[var(--radius)] border bg-card p-6">
        <div className="space-y-2"><Label>کد نصب</Label><Input dir="ltr" value={f.setupToken} onChange={(e) => setF({ ...f, setupToken: e.target.value })} /></div>
        <div className="space-y-2"><Label>نام شما</Label><Input value={f.fullName} onChange={(e) => setF({ ...f, fullName: e.target.value })} /></div>
        <div className="space-y-2"><Label>شماره موبایل</Label><Input dir="ltr" inputMode="tel" value={f.phone} onChange={(e) => setF({ ...f, phone: e.target.value })} /></div>
        {err && <p className="text-sm text-destructive">{err}</p>}
        <Button className="w-full" onClick={submit}>ساخت حساب مدیر</Button>
      </div>
    </div>
  );
}
