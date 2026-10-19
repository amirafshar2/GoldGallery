const fa = new Intl.NumberFormat("fa-IR");

/** Rial → Toman, Persian digits. */
export const toman = (rial: number | null | undefined) => (rial == null ? "—" : fa.format(Math.round(rial / 10)));
export const faNum = (n: number | string) => fa.format(Number(n));
export const grams = (g: number) => new Intl.NumberFormat("fa-IR", { minimumFractionDigits: 2, maximumFractionDigits: 3 }).format(g);
export const pct = (bp: number) => `${fa.format(bp / 100)}٪`;
export const faDate = (iso: string) => new Intl.DateTimeFormat("fa-IR-u-ca-persian", { dateStyle: "medium", timeStyle: "short" }).format(new Date(iso));

export const STATUS_FA: Record<string, string> = { AwaitingPayment: "در انتظار پرداخت", Paid: "پرداخت‌شده", Packed: "بسته‌بندی", Shipped: "ارسال‌شده", PickedUp: "تحویل حضوری", Delivered: "تحویل شد", Cancelled: "لغو شد", Refunded: "مرجوع" };
export const ROLE_FA: Record<string, string> = { Customer: "مشتری", Wholesale: "همکار عمده", Staff: "کارمند", Admin: "مدیر" };
