"use client";

import { createContext, useContext, useEffect, useSyncExternalStore } from "react";
import type { Site } from "@/lib/site";
import { makeT, type TextKey } from "@/lib/texts";
import { auth, restoreSession, type Me } from "@/lib/api";

type Ctx = { site: Site; t: (k: TextKey) => string };
const SiteCtx = createContext<Ctx | null>(null);

export function SiteProvider({ site, children }: { site: Site; children: React.ReactNode }) {
  useEffect(() => { restoreSession(); }, []);
  return <SiteCtx.Provider value={{ site, t: makeT(site.texts?.values) }}>{children}</SiteCtx.Provider>;
}

export function useSite() {
  const c = useContext(SiteCtx);
  if (!c) throw new Error("SiteProvider missing");
  return c;
}

export function useMe(): Me | null {
  return useSyncExternalStore(auth.subscribe, () => auth.me, () => null);
}
