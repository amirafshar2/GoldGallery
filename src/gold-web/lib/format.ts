const de = new Intl.NumberFormat("de-DE");
const eur = new Intl.NumberFormat("de-DE", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Money is stored as integer cents. Shows "1.234,56" (add "€" next to it in the UI). */
export const euro = (cents: number | null | undefined) => (cents == null ? "—" : eur.format(cents / 100));
export const num = (n: number | string) => de.format(Number(n));
export const grams = (g: number) => new Intl.NumberFormat("de-DE", { minimumFractionDigits: 2, maximumFractionDigits: 3 }).format(g);
export const pct = (bp: number) => `${de.format(bp / 100)} %`;
export const date = (iso: string) => new Intl.DateTimeFormat("de-DE", { dateStyle: "medium", timeStyle: "short" }).format(new Date(iso));

export const STATUS_LABEL: Record<string, string> = { AwaitingPayment: "Zahlung ausstehend", Paid: "Bezahlt", Packed: "Verpackt", Shipped: "Versendet", PickedUp: "Im Geschäft abgeholt", Delivered: "Zugestellt", Cancelled: "Storniert", Refunded: "Zurückerstattet" };
export const ROLE_LABEL: Record<string, string> = { Customer: "Kunde", Wholesale: "Großhandelspartner", Staff: "Mitarbeiter", Admin: "Admin" };
