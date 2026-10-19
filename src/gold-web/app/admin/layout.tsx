"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { LayoutDashboard, Palette, Coins, FolderTree, Store, Gem, ClipboardList, Type, Home, Phone, Globe, Rocket, ShieldCheck, Megaphone, ToggleLeft, Scale, UserRound, CalendarCheck } from "lucide-react";
import { restoreSession } from "@/lib/api";
import { useMe } from "@/components/site-provider";
import { OnboardingGuide } from "@/components/admin/onboarding";

const NAV: { href: string; label: string; icon: React.ComponentType<{ className?: string }>; admin?: boolean }[] = [
  { href: "/admin", label: "داشبورد", icon: LayoutDashboard },
  { href: "/admin/orders", label: "سفارش‌ها", icon: ClipboardList },
  { href: "/admin/products", label: "محصولات و قطعه‌ها", icon: Gem },
  { href: "/admin/categories", label: "دسته‌بندی‌ها", icon: FolderTree, admin: true },
  { href: "/admin/branches", label: "شعبه‌ها", icon: Store, admin: true },
  { href: "/admin/pricing", label: "نرخ طلا و اجرت", icon: Coins, admin: true },
  { href: "/admin/site/brand", label: "نام و لوگو", icon: ShieldCheck, admin: true },
  { href: "/admin/site/theme", label: "رنگ و ظاهر", icon: Palette, admin: true },
  { href: "/admin/site/contact", label: "تماس و آدرس", icon: Phone, admin: true },
  { href: "/admin/site/home", label: "صفحه‌ی اول", icon: Home, admin: true },
  { href: "/admin/site/trust", label: "مزیت‌ها", icon: Megaphone, admin: true },
  { href: "/admin/site/owner", label: "مدیر / مشاور", icon: UserRound, admin: true },
  { href: "/admin/site/installments", label: "خرید اقساطی", icon: CalendarCheck, admin: true },
  { href: "/admin/site/legal", label: "مجوزها و درباره‌ی ما", icon: Scale, admin: true },
  { href: "/admin/site/footer", label: "فوتر", icon: Type, admin: true },
  { href: "/admin/site/seo", label: "سئو", icon: Globe, admin: true },
  { href: "/admin/site/features", label: "بخش‌های فعال", icon: ToggleLeft, admin: true },
  { href: "/admin/site/texts", label: "همه‌ی متن‌ها", icon: Type, admin: true },
  { href: "/admin/site/publish", label: "انتشار سایت", icon: Rocket, admin: true },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const me = useMe();
  const path = usePathname();
  const router = useRouter();
  const [ready, setReady] = useState(false);
  useEffect(() => {
    restoreSession().then((u) => {
      if (!u || (u.role !== "Admin" && u.role !== "Staff")) router.replace("/login?next=/admin");
      else setReady(true);
    });
  }, [router]);
  if (!ready || !me) return <div className="p-10 text-center text-muted-foreground">در حال بارگذاری…</div>;
  const isAdmin = me.role === "Admin";

  return (
    <div className="mx-auto flex max-w-[1500px] flex-col gap-6 px-4 py-6 lg:flex-row lg:items-start">
      <nav className="flex gap-1 overflow-x-auto pb-2 lg:sticky lg:top-36 lg:w-56 lg:shrink-0 lg:flex-col lg:overflow-visible" aria-label="منوی مدیریت">
        {NAV.filter((n) => isAdmin || !n.admin).map((n) => (
          <Link key={n.href} href={n.href} className={`flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-sm whitespace-nowrap ${path === n.href ? "bg-primary text-primary-foreground" : "hover:bg-accent"}`}>
            <n.icon className="size-4" /> {n.label}
          </Link>
        ))}
      </nav>
      <div className="min-w-0 flex-1">{children}</div>
      {isAdmin && <div className="hidden lg:block lg:w-80 lg:shrink-0"><OnboardingGuide inline /></div>}
      {isAdmin && <div className="lg:hidden"><OnboardingGuide /></div>}
    </div>
  );
}
