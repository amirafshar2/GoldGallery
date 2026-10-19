"use client";

/** Tiny API client. Access token lives in memory only; refresh token is an HttpOnly cookie. */
let accessToken: string | null = null;
let refreshing: Promise<boolean> | null = null;
const listeners = new Set<() => void>();

export type Me = { id: string; phone: string; fullName: string; role: "Customer" | "Wholesale" | "Staff" | "Admin"; isDemo: boolean };
let me: Me | null = null;

export const auth = {
  get me() { return me; },
  subscribe(fn: () => void) { listeners.add(fn); return () => { listeners.delete(fn); }; },
  set(token: string | null, user: Me | null) { accessToken = token; me = user; listeners.forEach((l) => l()); },
};

export class ApiError extends Error {
  constructor(public status: number, message: string, public body?: unknown) { super(message); }
}

async function refresh(): Promise<boolean> {
  refreshing ??= (async () => {
    try {
      const r = await fetch("/api/v1/auth/refresh", { method: "POST", credentials: "include" });
      if (!r.ok) { auth.set(null, null); return false; }
      const j = await r.json();
      auth.set(j.accessToken, j.user);
      return true;
    } finally { setTimeout(() => (refreshing = null), 0); }
  })();
  return refreshing;
}

export async function api<T = unknown>(path: string, init: RequestInit & { json?: unknown } = {}, retry = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.json !== undefined) headers.set("content-type", "application/json");
  if (accessToken) headers.set("authorization", `Bearer ${accessToken}`);
  const res = await fetch(`/api/v1${path}`, {
    ...init,
    headers,
    body: init.json !== undefined ? JSON.stringify(init.json) : init.body,
    credentials: "include",
  });
  if (res.status === 401 && retry && (await refresh())) return api<T>(path, init, false);
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  const body = text ? JSON.parse(text) : undefined;
  if (!res.ok) {
    const errs = body?.errors ? Object.values(body.errors).flat().join(" ") : "";
    throw new ApiError(res.status, [body?.title, errs].filter(Boolean).join(" — ") || "خطا", body);
  }
  return body as T;
}

export async function restoreSession() {
  // Only ask the server when a session can exist (hint cookie set at login) — avoids a 401 for every visitor.
  const hasSession = typeof document !== "undefined" && document.cookie.split("; ").includes("gold_session=1");
  if (!accessToken && hasSession) await refresh();
  return me;
}

export async function demoLogin(role: string) {
  const r = await api<{ accessToken: string; user: Me }>("/auth/demo-login", { method: "POST", json: { role } });
  auth.set(r.accessToken, r.user);
  return r.user;
}

export async function logout() {
  await api("/auth/logout", { method: "POST" }).catch(() => {});
  auth.set(null, null);
}

export async function upload(file: File): Promise<string> {
  const fd = new FormData();
  fd.append("file", file);
  const r = await api<{ url: string }>("/admin/media", { method: "POST", body: fd });
  return r.url;
}
