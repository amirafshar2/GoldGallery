"use client";

export type CartItem = { pieceId: string; productName: string; slug: string; grams: number; priceRial: number };
const KEY = "gold-cart";

export function readCart(): CartItem[] {
  try { return JSON.parse(localStorage.getItem(KEY) ?? "[]"); } catch { return []; }
}
export function writeCart(items: CartItem[]) {
  try { localStorage.setItem(KEY, JSON.stringify(items)); } catch { /* storage unavailable */ }
  window.dispatchEvent(new Event("cart"));
}
export function addToCart(i: CartItem) {
  const c = readCart().filter((x) => x.pieceId !== i.pieceId);
  writeCart([...c, i].slice(0, 10));
}
