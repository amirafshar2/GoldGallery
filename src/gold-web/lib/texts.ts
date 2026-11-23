/**
 * Default UI labels. EVERY label can be overridden from the admin panel (site.texts.values[key]).
 * Keep keys stable — admins edit them by key.
 */
export const DEFAULT_TEXTS = {
  "nav.home": "Start",
  "nav.shop": "Shop",
  "nav.price": "Goldpreis heute",
  "nav.buyback": "Altgold verkaufen",
  "nav.contact": "Kontakt",
  "nav.installments": "Ratenkauf",
  "nav.login": "Anmelden",
  "nav.account": "Mein Konto",
  "nav.admin": "Admin-Panel",
  "nav.cart": "Warenkorb",
  "rate.label": "750er Gold – pro Gramm",
  "rate.toman": "€",
  "rate.paused": "Onlineverkauf kurz pausiert – der Kurs wird aktualisiert",
  "card.from": "ab",
  "card.wage": "Arbeitslohn",
  "shop.title": "Shop",
  "shop.filters": "Filter",
  "shop.empty": "Keine Produkte gefunden.",
  "product.pieces": "Verfügbare Stücke",
  "product.branch": "Filiale",
  "product.weight": "Gewicht (g)",
  "product.size": "Größe",
  "product.wage": "Arbeitslohn",
  "product.price": "Preis (€)",
  "product.add": "In den Warenkorb",
  "product.breakdown": "Preisaufschlüsselung",
  "product.formula": "Preis = Materialwert + Arbeitslohn + Gewinn + Steuer (nur auf Arbeitslohn und Gewinn)",
  "cart.title": "Warenkorb",
  "cart.empty": "Der Warenkorb ist leer.",
  "cart.lock": "Preis sichern und bezahlen",
  "cart.locked": "Der Preis ist gesichert bis:",
  "cart.pay": "Bezahlen",
  "buyback.title": "Was ist mein Gold wert?",
  "buyback.hint": "Beim Ankauf von Altgold fällt kein Arbeitslohn an. Die endgültige Bewertung erfolgt im Geschäft auf der Waage.",
  "login.title": "Anmelden / Registrieren",
  "login.phone": "Handynummer",
  "login.send": "Code senden",
  "login.code": "5-stelliger Code",
  "login.verify": "Anmelden",
  "login.demo": "Schnell-Login (Demo)",
  "demo.banner": "Demo-Modus – alle Daten sind Beispiele",
  "showcase.ribbon": "Demo-Version – Website-Entwurf. Zahlungen sind nur Tests, es werden keine Bestellungen ausgeführt.",
  "wa.float": "Beratung per WhatsApp",
  "wa.order": "Per WhatsApp bestellen",
  "footer.rights": "Alle Rechte vorbehalten.",
} as const;

export type TextKey = keyof typeof DEFAULT_TEXTS;

export function makeT(overrides: Record<string, string> | undefined) {
  return (key: TextKey) => overrides?.[key] || DEFAULT_TEXTS[key];
}
