"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Repeat } from "lucide-react";
import { demoLogin } from "@/lib/api";
import { useMe } from "./site-provider";

export const DEMO_ROLES = [
  { role: "customer", label: "Kunde", hint: "Kaufen und Bestellungen verfolgen", to: "/account" },
  { role: "wholesale", label: "Großhandelspartner", hint: "Goldkonto und Großbestellungen", to: "/account" },
  { role: "staff", label: "Mitarbeiter", hint: "Stücke, Bestellungen, Termine", to: "/admin" },
  { role: "admin", label: "Admin", hint: "Kurs, Inhalte, Einstellungen", to: "/admin" },
] as const;

/** Floating "switch role" button — only for demo sessions (presentations). */
export function RoleSwitcher() {
  const me = useMe();
  const [open, setOpen] = useState(false);
  const router = useRouter();
  if (!me?.isDemo) return null;
  return (
    <div className="fixed bottom-4 left-4 z-50">
      {open && (
        <div className="mb-2 grid w-56 gap-1 rounded-2xl border bg-card p-2 shadow-2xl">
          {DEMO_ROLES.map((r) => (
            <button key={r.role} data-testid={`switch-${r.role}`} onClick={async () => { await demoLogin(r.role); setOpen(false); router.push(r.to); }}
              className={`rounded-xl px-3 py-2 text-left text-sm hover:bg-accent ${me.role.toLowerCase() === r.role ? "text-primary" : ""}`}>
              {r.label}
            </button>
          ))}
        </div>
      )}
      <button onClick={() => setOpen(!open)} className="flex items-center gap-2 rounded-full bg-primary px-4 py-3 text-sm font-semibold text-primary-foreground shadow-2xl">
        <Repeat className="size-4" /> Rolle wechseln
      </button>
    </div>
  );
}
