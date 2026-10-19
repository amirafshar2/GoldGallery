/**
 * Default UI labels. EVERY label can be overridden from the admin panel (site.texts.values[key]).
 * Keep keys stable — admins edit them by key.
 */
export const DEFAULT_TEXTS = {
  "nav.home": "خانه",
  "nav.shop": "فروشگاه",
  "nav.price": "قیمت روز",
  "nav.buyback": "فروش طلای کهنه",
  "nav.contact": "تماس",
  "nav.installments": "خرید اقساطی",
  "nav.login": "ورود",
  "nav.account": "حساب من",
  "nav.admin": "پنل مدیریت",
  "nav.cart": "سبد خرید",
  "rate.label": "طلای ۱۸ عیار — هر گرم",
  "rate.toman": "تومان",
  "rate.paused": "فروش آنلاین موقتاً متوقف است — نرخ در حال به‌روزرسانی",
  "card.from": "شروع از",
  "card.wage": "اجرت",
  "shop.title": "فروشگاه",
  "shop.filters": "فیلترها",
  "shop.empty": "محصولی پیدا نشد.",
  "product.pieces": "قطعه‌های موجود",
  "product.branch": "شعبه",
  "product.weight": "وزن (گرم)",
  "product.size": "سایز",
  "product.wage": "اجرت",
  "product.price": "قیمت (تومان)",
  "product.add": "افزودن به سبد",
  "product.breakdown": "ریز قیمت",
  "product.formula": "قیمت = طلای خام + اجرت + سود + مالیات (فقط روی اجرت و سود)",
  "cart.title": "سبد خرید",
  "cart.empty": "سبد خرید خالی است.",
  "cart.lock": "قیمت را قفل کن و پرداخت کن",
  "cart.locked": "قیمت تا این زمان قفل است:",
  "cart.pay": "پرداخت",
  "buyback.title": "ارزش طلای من چقدر است؟",
  "buyback.hint": "در خرید طلای کهنه اجرت محاسبه نمی‌شود. ارزیابی نهایی حضوری و با ترازو انجام می‌شود.",
  "login.title": "ورود / ثبت‌نام",
  "login.phone": "شماره موبایل",
  "login.send": "ارسال کد",
  "login.code": "کد ۵ رقمی",
  "login.verify": "ورود",
  "login.demo": "ورود سریع نمایشی",
  "demo.banner": "حالت نمایشی — داده‌ها نمونه هستند",
  "showcase.ribbon": "نسخه‌ی نمایشی — طراحی پیشنهادی وب‌سایت. پرداخت‌ها آزمایشی‌اند و سفارشی ثبت نمی‌شود.",
  "wa.float": "مشاوره در واتساپ",
  "wa.order": "سفارش در واتساپ",
  "footer.rights": "تمام حقوق محفوظ است.",
} as const;

export type TextKey = keyof typeof DEFAULT_TEXTS;

export function makeT(overrides: Record<string, string> | undefined) {
  return (key: TextKey) => overrides?.[key] || DEFAULT_TEXTS[key];
}
