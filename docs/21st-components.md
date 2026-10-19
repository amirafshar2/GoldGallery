# ثبت کامپوننت‌های 21st.dev (اجباری)

> هر کامپوننتی که در `gold-web` هست باید اینجا ثبت شده باشد؛ وگرنه حذف شود.

| بخش | کامپوننت | نویسنده | لینک | لایسنس | تاریخ | تغییرات |
|---|---|---|---|---|---|---|
| پایه‌ی UI (Button, Input, Card, Table, Badge, Label, Textarea, Separator, Skeleton) | shadcn/ui new-york-v4 | shadcn | github.com/shadcn-ui/ui | MIT | ۱۴۰۵/۰۷/۲۶ | پایه‌ای که خود 21st روی آن ساخته شده؛ import به `@/lib/utils` |
| موشن ورود (کارت‌ها، تیترها، مزیت‌ها، صفحه‌ی محصول) | Blur Fade | magicui | https://21st.dev/magicui · github.com/magicuidesign/magicui | MIT | ۱۴۰۵/۰۷/۲۷ | فقط `motion/react` → `framer-motion` — `components/ui21/blur-fade.tsx` |
| تیتر هیرو (کلمه‌به‌کلمه — حرف‌به‌حرف فارسی را می‌شکند) | Text Animate | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | همان — `ui21/text-animate.tsx` |
| نوار دسته‌ها با عکس | Marquee | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | داخل `dir="ltr"` برای RTL — `ui21/marquee.tsx` |
| عدد نرخ زنده | Number Ticker | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | + پراپ `locale` برای ارقام فارسی — `ui21/number-ticker.tsx` |
| قاب نوری (نرخ، کارت ویژه) | Border Beam | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | `ui21/border-beam.tsx` |
| دکمه‌ی هیرو | Shimmer Button | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | `ui21/shimmer-button.tsx` |
| ذره‌بین عکس 4K محصول | Lens | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | `ui21/lens.tsx` |
| نوار پیشرفت اسکرول هدر | Scroll Progress | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | origin-right برای RTL — `ui21/scroll-progress.tsx` |
| (آماده، هنوز استفاده نشده) | Progressive Blur | magicui | همان | MIT | ۱۴۰۵/۰۷/۲۷ | `ui21/progressive-blur.tsx` |
| ⚠ هیرو اسکرول عکس | Zoom Parallax (@efferd) — **بازسازی با همان قرارداد و چیدمان دمو**؛ Component.tsx در 21st بدون ورود قفل است | efferd | https://21st.dev/@efferd/components/zoom-parallax | — | ۱۴۰۵/۰۷/۲۷ | بعد از ورود امیر به 21st با کد اصلی عوض شود — `slots/zoom-parallax.tsx` |
| ⚠ گالری اسکرول افقی | الگوی Horizontal Scroll / Immersive Scroll Gallery — **بازسازی** (کد قفل) | — | https://21st.dev/@uilayout.contact/components/horizontal-scroll | — | ۱۴۰۵/۰۷/۲۷ | RTL + موبایل swipe — `gallery/horizontal-gallery.tsx` |
| ⚠ هدر، فوتر، فرم‌های پنل | روی shadcn + تم جدید — با Navigation / Footers از 21st عوض شود | — | دسته‌های 21st | — | ۱۴۰۵/۰۷/۲۷ | منتظر ورود |

### عکس‌ها (کانوا)
- ۱۶ عکس محصول + ۷ عکس هیرو با Canva AI ساخته شد، در دیزاین `DAHYcGHsSwQ` گذاشته شد و با عرض **3840** خروجی گرفته شد (upscale، نه عکاسی بومی 4K).
- `public/img/4k/*` = اصل 4K (برای ذره‌بین صفحه‌ی محصول) · `public/img/p/*` = 1200px برای کارت‌ها · `public/img/hero/*` = 2560px. نگاشت در `docs/canva-media.json`.

## کاندیداهای انتخاب‌شده (از تحقیق)
| بخش | کامپوننت | لینک |
|---|---|---|
| هیرو اسکرول عکس | Zoom Parallax | https://21st.dev/@efferd/components/zoom-parallax |
| سرویس عروس | Scroll Reveal Image (MIT) | https://21st.dev/@unlumen/components/scroll-reveal-image |
| از طرح تا انگشتر | Immersive Scroll Gallery | https://21st.dev/@ishamsu/components/immersive-scroll-gallery |
| کالکشن‌ها | Horizontal Scroll | https://21st.dev/@uilayout.contact/components/horizontal-scroll |
| بنر دسته‌ها | Parallax Image | https://21st.dev/@pulkitxm/components/parallax-image |
