"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { Menu, ShoppingBag, User, X } from "lucide-react";
import { useSite, useMe } from "./site-provider";
import { RateTicker } from "./rate-ticker";
import { readCart } from "@/lib/cart";
import { ScrollProgress } from "./ui21/scroll-progress";

export function Header() {
  const { site, t } = useSite();
  const me = useMe();
  const [open, setOpen] = useState(false);
  const [count, setCount] = useState(0);
  useEffect(() => {
    const u = () => setCount(readCart().length);
    u(); window.addEventListener("cart", u); window.addEventListener("storage", u);
    return () => { window.removeEventListener("cart", u); window.removeEventListener("storage", u); };
  }, []);

  const links = [
    { href: "/", label: t("nav.home"), on: true },
    { href: "/shop", label: t("nav.shop"), on: site.features.shop },
    { href: "/price", label: t("nav.price"), on: true },
    { href: "/installments", label: t("nav.installments"), on: site.installments?.enabled },
    { href: "/sell-old-gold", label: t("nav.buyback"), on: site.features.buyBack },
    { href: "/contact", label: t("nav.contact"), on: true },
  ].filter((l) => l.on);
  const panel = me?.role === "Admin" || me?.role === "Staff" ? "/admin" : "/account";

  return (
    <header className="sticky top-0 z-40 border-b bg-background/85 backdrop-blur-xl">
      {me?.isDemo && <div className="bg-primary px-4 py-1 text-center text-xs text-primary-foreground">{t("demo.banner")}</div>}
      <div className="velvet flex justify-center px-4 py-1.5"><RateTicker compact /></div>
      <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3">
        <Link href="/" className="flex min-w-0 items-center gap-3">
          {site.brand.logoUrl && <img src={site.brand.logoUrl} alt="" className="h-9 w-auto" />}
          <span className="truncate text-xl font-extrabold tracking-tight gold-text">{site.brand.name || "نام فروشگاه"}</span>
        </Link>
        <nav className="hidden items-center gap-7 text-sm md:flex">
          {links.map((l) => <Link key={l.href} href={l.href} className="relative text-muted-foreground transition-colors hover:text-foreground after:absolute after:-bottom-1 after:right-0 after:h-px after:w-0 after:bg-primary after:transition-all hover:after:w-full">{l.label}</Link>)}
        </nav>
        <div className="flex items-center gap-2">
          <Link href="/cart" aria-label={t("nav.cart")} className="relative rounded-full p-2 hover:bg-accent">
            <ShoppingBag className="size-5" />
            {count > 0 && <span className="absolute -top-0.5 -left-0.5 grid size-5 place-items-center rounded-full bg-primary text-[10px] text-primary-foreground num">{count}</span>}
          </Link>
          <Link href={me ? panel : "/login"} className="flex items-center gap-1 rounded-full border px-3 py-1.5 text-sm hover:bg-accent">
            <User className="size-4" /> {me ? (me.role === "Admin" || me.role === "Staff" ? t("nav.admin") : t("nav.account")) : t("nav.login")}
          </Link>
          <button className="rounded-full p-2 hover:bg-accent md:hidden" onClick={() => setOpen(!open)} aria-label="منو">
            {open ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
        </div>
      </div>
      {open && (
        <nav className="grid gap-1 border-t px-4 py-3 md:hidden">
          {links.map((l) => <Link key={l.href} href={l.href} onClick={() => setOpen(false)} className="rounded-lg px-3 py-3 hover:bg-accent">{l.label}</Link>)}
        </nav>
      )}
      <ScrollProgress className="absolute top-auto bottom-0 h-0.5 origin-right bg-linear-to-l from-primary via-gold-hi to-primary" />
    </header>
  );
}
