"use client";

import { usePathname } from "next/navigation";
import { MessageCircle } from "lucide-react";
import { useSite } from "./site-provider";
import { waLink } from "@/lib/utils";

/** Floating WhatsApp button — Iranian gold shops take most orders there. Hidden in the admin panel. */
export function WhatsAppFloat() {
  const { site, t } = useSite();
  const path = usePathname();
  const n = site.contact.whatsApp;
  if (!n || path.startsWith("/admin") || path.startsWith("/setup")) return null;
  return (
    <a href={waLink(n, `سلام ${site.brand.name}`)} target="_blank" rel="noopener noreferrer" data-testid="wa-float"
      className="group fixed bottom-5 right-5 z-40 flex items-center gap-2 rounded-full bg-[#1f9d55] p-3.5 text-white shadow-[0_12px_40px_-8px_rgba(31,157,85,.7)] transition hover:pr-5">
      <span className="absolute inset-0 -z-10 animate-ping rounded-full bg-[#1f9d55]/40 [animation-duration:2.5s]" />
      <MessageCircle className="size-6" />
      <span className="max-w-0 overflow-hidden whitespace-nowrap text-sm font-semibold transition-all duration-300 group-hover:max-w-40">{t("wa.float")}</span>
    </a>
  );
}
