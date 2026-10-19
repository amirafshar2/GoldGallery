import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** wa.me link from an Iranian mobile (09xx…) with an optional prefilled message. */
export function waLink(number: string, text = "") {
  const d = (number || "").replace(/\D/g, "");
  const intl = d.startsWith("0") ? "98" + d.slice(1) : d;
  return `https://wa.me/${intl}${text ? `?text=${encodeURIComponent(text)}` : ""}`;
}
