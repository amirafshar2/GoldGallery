using System.Text.Json;
using Gold.Application.Site;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;

namespace Gold.Infrastructure.Persistence;

/// <summary>
/// Showcase profile for گالری طلای مروجی (Shiraz). Public facts come from the shop's own Instagram pages
/// (@moravejigoldgallery, @esmaiil_moraveji): address, phone numbers, installment terms, product names and weights.
/// Photos are Canva AI images; prices are computed from the sample rate. Everything is editable in the admin panel.
/// </summary>
public sealed partial class DataSeeder
{
    private void SeedMoraveji(DateTimeOffset now)
    {
        var cats = new Dictionary<string, Category>
        {
            ["rings"] = Category.Create("rings", "انگشتر", 1),
            ["bracelets"] = Category.Create("bracelets", "دستبند و النگو", 2),
            ["necklaces"] = Category.Create("necklaces", "گردنبند و آویز", 3),
            ["earrings"] = Category.Create("earrings", "گوشواره", 4),
            ["sets"] = Category.Create("sets", "سرویس و نیم‌ست عروس", 5),
            ["men"] = Category.Create("men", "مجموعه‌ی مردانه", 6),
            ["watches"] = Category.Create("watches", "ساعت طلا", 7),
            ["invest"] = Category.Create("invest", "آبشده، سکه و شمش", 8),
        };
        db.Categories.AddRange(cats.Values);

        var shop = Branch.Create("گالری طلای مروجی", "شیراز، بلوار بعثت، نبش کوچه ۱۶");
        db.Branches.Add(shop);

        // (slug, name, category, audience, color, wage %, featured, karat, grams, sizes, image, description)
        var k18 = Karat.K18;
        var products = new (string Slug, string Name, string Cat, Audience A, GoldColor C, int Wage, bool Featured, Karat K, decimal[] Grams, string[] Sizes, string Img, string Desc)[]
        {
            ("clover-bracelet", "دستبند چهارپر", "bracelets", Audience.Women, GoldColor.Yellow, 12, true, k18, [15.51m], ["17cm"], "mj-clover",
                "دستبند گل چهارپر با جلوه‌ی لوکس؛ ست با انگشتر چهارپر."),
            ("clover-ring", "انگشتر گل چهارپر", "rings", Audience.Women, GoldColor.Yellow, 12, false, k18, [5.90m], ["56"], "mj-clover",
                "انگشتر گل چهارپر 🍀 — ست با دستبند چهارپر."),
            ("ring-x", "انگشتر ضربدری X", "rings", Audience.Women, GoldColor.Yellow, 15, false, k18, [3.47m], ["54"], "mj-ring-x",
                "مینیمال، اما تأثیرگذار؛ برای استایلی که ساده است ولی فراموش نمی‌شود."),
            ("ring-solitaire", "انگشتر سولیتر", "rings", Audience.Women, GoldColor.Yellow, 18, true, k18, [2.85m, 3.10m], ["53", "56"], "mj-ring-solitaire",
                "درخشش عشق، در سادگی یک سولیتر. گاهی یک نگین، تمام حرف دل را می‌زند."),
            ("earring-nili", "گوشواره نیلی", "earrings", Audience.Women, GoldColor.Yellow, 16, true, k18, [7.79m], [""], "mj-earring-nili",
                "گوشواره‌ی آویز با نگین نیلی؛ رنگی و خاص، مناسب هدیه."),
            ("earring-butterfly", "گوشواره پروانه", "earrings", Audience.Women, GoldColor.Yellow, 16, false, k18, [4.95m], [""], "mj-earring-butterfly",
                "گوشواره‌ی پروانه‌ای با بال‌های مشبک ظریف 🦋"),
            ("pendant-butterfly", "پلاک آویز پروانه", "necklaces", Audience.Women, GoldColor.Yellow, 17, false, k18, [1.05m, 1.20m], ["", ""], "pendant-butterfly",
                "پلاک آویز سبک؛ وقتشه طلا بخری، حتی اگه انتخابت طلای سبک باشه."),
            ("necklace-name", "گردنبند اسم سفارشی", "necklaces", Audience.Women, GoldColor.Yellow, 20, false, k18, [2.10m, 2.65m], ["45cm", "50cm"], "necklace-name",
                "اسم دلخواه شما با خط زیبا؛ سفارش از واتساپ یا دایرکت."),
            ("set-twotone", "سرویس ترکیبی طلای سفید و زرد", "sets", Audience.Women, GoldColor.Yellow, 14, true, k18, [52.66m], ["ست کامل"], "mj-set-twotone",
                "گردنبند ۳۲٫۳۲ + دستبند ۱۵٫۳۴ + گوشواره ۵ گرم — ترکیب زیبای طلای سفید و زرد."),
            ("set-tiffany", "سرویس تیفانی با قفل اصلی", "sets", Audience.Women, GoldColor.White, 15, false, k18, [18.40m], ["ست"], "necklace-tiffany",
                "سرویس زیبای تیفانی با قفل اصلی."),
            ("halfset-bridal", "نیم‌ست عروس سبک", "sets", Audience.Women, GoldColor.Yellow, 16, true, k18, [14.20m], ["نیم‌ست"], "mj-halfset-bridal",
                "نیم‌ست‌های خوشگل‌مون با جلوه‌ی خیلی زیاد اما وزن مناسب 👰🏻‍♀️"),
            ("set-bridal-royal", "سرویس عروس رویال", "sets", Audience.Women, GoldColor.Yellow, 18, false, k18, [42.50m], ["ست کامل"], "set-bridal-royal",
                "سرویس کامل عروس؛ گردنبند، گوشواره و دستبند."),
            ("watch-women", "ساعت طلای زنانه", "watches", Audience.Women, GoldColor.Yellow, 10, true, k18, [39.44m], [""], "mj-watches",
                "Luxury couple watches — یک امضای لوکس روی مچ دست. ⌚️"),
            ("watch-men", "ساعت طلای مردانه", "watches", Audience.Men, GoldColor.Yellow, 10, false, k18, [67.70m], [""], "mj-watches",
                "برای کسانی که ثانیه‌ها را نمی‌گذرانند، بلکه مالک آن هستند."),
            ("bracelet-men-cuban", "دستبند مردانه کارتیه", "men", Audience.Men, GoldColor.Yellow, 8, false, k18, [12.40m, 14.10m], ["21cm", "22cm"], "bracelet-men-cuban",
                "دستبند مردانه‌ی حلقه‌درشت."),
            ("ring-men-signet", "انگشتر مردانه", "men", Audience.Men, GoldColor.Yellow, 10, false, k18, [6.20m], ["64"], "ring-men-signet",
                "انگشتر مردانه‌ی صاف و سنگین."),
            ("abshodeh", "طلای آبشده", "invest", Audience.Unisex, GoldColor.Yellow, 0, true, k18, [4.608m, 23.04m, 46.08m], ["۱ مثقال", "۵ مثقال", "۱۰ مثقال"], "mj-abshodeh",
                "بهترین طلا برای سرمایه‌گذاری؛ بدون اجرت ساخت. از فروشنده‌ی مجاز بخرید."),
            ("coin-parsian", "سکه پارسیان", "invest", Audience.Unisex, GoldColor.Yellow, 3, false, Karat.Coin, [0.5m, 1.0m, 1.5m], ["۰٫۵ گرمی", "۱ گرمی", "۱٫۵ گرمی"], "coin-parsian-1g",
                "یکی از گزینه‌های محبوب سرمایه‌گذاری با مبالغ کمتر."),
            ("bullion", "شمش طلا", "invest", Audience.Unisex, GoldColor.Yellow, 1, false, Karat.Bullion995, [5m, 10m], ["۵ گرم", "۱۰ گرم"], "mj-bullion",
                "شمش استاندارد برای سرمایه‌گذاری بلندمدت."),
            ("bangle-classic", "النگو تراش‌دار", "bracelets", Audience.Women, GoldColor.Yellow, 9, false, k18, [9.12m, 10.30m], ["62", "64"], "bangle-classic",
                "النگوی کلاسیک تراش‌دار."),
        };

        var barcode = 500_000;
        foreach (var p in products)
        {
            var prod = Product.Create(p.Slug, p.Name, cats[p.Cat].Id, p.A, p.C,
                WageRule.PercentOfRaw(Percentage.FromPercent(p.Wage)), p.Desc, $"/img/p/{p.Img}.jpg", p.Featured);
            db.Products.Add(prod);
            for (var i = 0; i < p.Grams.Length; i++)
            {
                var stone = p.Slug == "ring-solitaire" ? Money.FromToman(9_500_000) : Money.Zero;
                db.Pieces.Add(Piece.Create(prod.Id, shop.Id, $"M{barcode++}", Weight.FromGrams(p.Grams[i]), p.K,
                    p.Sizes[i], stone.Rial > 0 ? Weight.FromGrams(0.10m) : Weight.Zero, stone));
            }
        }

        void Put(string key, object value) =>
            db.SiteSettings.Add(SiteSetting.Create(key, JsonSerializer.Serialize(value, value.GetType(), SiteService.Json), now, null));

        const string address = "شیراز، بلوار بعثت، نبش کوچه ۱۶";
        Put(SiteKeys.Brand, new BrandSection("گالری طلای مروجی", "طلا · سکه · آبشده · طلای اقساطی — شیراز", ""));
        Put(SiteKeys.Theme, new ThemeSection(Primary: "#8C6A2E"));
        Put(SiteKeys.Contact, new ContactSection(Phone: "09129175255", Mobile: "09129172883", WhatsApp: "09129172883",
            Instagram: "moravejigoldgallery", Address: address, City: "شیراز",
            MapUrl: "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString("گالری طلای مروجی " + address)));
        Put(SiteKeys.Legal, new LegalSection(AboutText:
            "گالری طلا و جواهرات مروجی در شیراز، با مدیریت اسماعیل مروّجی و بیش از ۱۰ سال تجربه در بازار طلا، سکه و ارز. " +
            "خرید و فروش طلای آبشده، سکه و شمش، زیورآلات زنانه و مردانه، سرویس عروس و خرید اقساطی طلا. اعتبار ما به اعداد نیست، به اعتماد شماست."));
        Put(SiteKeys.Home, new HomeSection(
            new HeroSection("طلای مروجی؛ سرمایه‌ای که می‌درخشد",
                "خرید و فروش طلای آبشده، سکه، شمش و زیورآلات — با مشاوره‌ی تخصصی و خرید اقساطی بدون سود", "ورود به ویترین", "/shop",
                ["/img/hero/mj-boutique.jpg", "/img/hero/hand-rings.jpg", "/img/hero/neck-necklace.jpg", "/img/hero/boutique.jpg",
                 "/img/hero/goldsmith.jpg", "/img/hero/rose-ring.jpg", "/img/hero/chain-macro.jpg"]),
            [
                new("rate-ticker", true, 1, "قیمت لحظه‌ای طلا"),
                new("categories", true, 2, "ویترین مروجی"),
                new("owner", true, 3, "اسماعیل مروّجی، مشاور شما", "قبل از خرید، با کسی حرف بزنید که بازار را می‌شناسد"),
                new("installments", true, 4, "طلا بخرید، قسطی بپردازید"),
                new("new-arrivals", true, 5, "تازه‌های ویترین"),
                new("gallery", true, 6, "منتخب گالری", "با اسکرول، ویترین را ورق بزنید"),
                new("story", true, 7, "اعتبار ما به اعداد نیست، به اعتماد شماست", "«سرمایه‌ی ما شمایید.» هر قطعه با وزن واقعی، عیار تضمینی و اجرت شفاف؛ قیمت از نرخ لحظه‌ای ساخته می‌شود."),
                new("buy-back", true, 8, "طلای کهنه یا آبشده‌ات را بفروش"),
                new("custom-order", true, 9, "پلاک اسم و سفارش ساخت"),
                new("trust", true, 10, "چرا گالری مروجی؟"),
                new("testimonials", false, 11, "رضایت شما"),
                new("contact", true, 12, "به گالری سر بزنید"),
            ]));
        Put(SiteKeys.Trust, new TrustSection(
        [
            new("shield", "فروشنده‌ی مجاز و معتبر", "خرید آبشده و سکه از مرکز دارای مجوز"),
            new("receipt", "اقساط بدون سود", "یک‌چهارم نقد، باقی در ۵ قسط ماهانه"),
            new("truck", "خرید آنلاین، ارسال سریع", "پرداخت امن و تحویل درب منزل"),
            new("sparkles", "قرعه‌کشی سکه پارسیان", "هر خرید، یک قدم نزدیک‌تر به جایزه"),
        ]));
        Put(SiteKeys.Footer, new FooterSection("گالری طلا و جواهرات مروجی — " + address + ".",
            [new("خرید اقساطی", "/installments"), new("قیمت روز", "/price"), new("تماس با ما", "/contact")]));
        Put(SiteKeys.Seo, new SeoSection("گالری طلای مروجی | طلا، سکه، آبشده و طلای اقساطی در شیراز",
            "خرید و فروش طلای آبشده، سکه، شمش و زیورآلات با قیمت لحظه‌ای و خرید اقساطی بدون سود — شیراز، بلوار بعثت."));
        Put(SiteKeys.Features, new FeaturesSection(Installments: true));
        Put(SiteKeys.Publish, new PublishSection(true));
        Put(SiteKeys.Owner, new OwnerSection(
            Enabled: true,
            Name: "اسماعیل مروّجی",
            Role: "مدیر گالری و مشاور خرید و سرمایه‌گذاری طلا",
            PhotoUrl: "/img/moraveji/owner.jpg",
            Quote: "خیال راحت سرمایه‌گذاری کنید. اعتبار ما به اعداد نیست، به اعتماد شماست.",
            Highlights: ["بیش از ۱۰ سال تجربه در بازار", "تخصص در طلا، سکه و ارز", "مشاوره‌ی تخصصی خرید و فروش هوشمندانه"],
            Instagram: "esmaiil_moraveji",
            Followers: "۶۷٫۷ هزار",
            Reels:
            [
                new("خیال راحت سرمایه‌گذاری کنید", "https://www.instagram.com/reel/DQroTYuEUm1/"),
                new("اگر وقتی طلا ۷ میلیون بود قسطی خریده بودید…", "https://www.instagram.com/reel/DRd6_mcingS/"),
                new("با خانواده‌ی طلای مروجی آشنا شوید", "https://www.instagram.com/reel/DPwRD0ECjvY/"),
                new("با خرید طلا ارزش سرمایه‌تان را حفظ کنید", "https://www.instagram.com/reel/DSSf0XHDNVQ/"),
            ],
            Tips:
            [
                new("طلای نقد یا فردایی؟", "قبل از فروش، تفاوت تحویل امروز و فردایی را بدانید تا انتخاب مطمئن‌تری داشته باشید."),
                new("دو طلا، یک وزن؛ چرا قیمت فرق دارد؟", "فرق در اجرت ساخت است؛ طراحی خاص و ظرافت بیشتر، اجرت بیشتری دارد."),
                new("آبشده برای سرمایه‌گذاری", "طلای آبشده اجرت ساخت ندارد و برای حفظ ارزش پول انتخاب هوشمندانه‌ای است."),
                new("نگین را با وزن طلا نخرید", "فروش نگین انگشتر با وزن طلا منصفانه نیست؛ وزن نگین باید جدا حساب شود."),
                new("کم‌کم بخرید، هوشمندانه بخرید", "لازم نیست از اول بزرگ بخرید؛ با مبلغ کم هم می‌شود شروع کرد."),
                new("از فروشنده‌ی مجاز بخرید", "قبل از خرید آبشده، مجوز و اعتبار فروشگاه را بررسی کنید."),
            ],
            CtaText: "مشاوره‌ی رایگان با آقای مروّجی"));
        Put(SiteKeys.Installments, new InstallmentsSection(
            Enabled: true, MaxRial: 3_000_000_000, DownPercent: 25, Months: 5, MonthlyFeePercent: 0,
            Title: "طلا بخرید، قسطی بپردازید",
            Note: "تا سقف ۳۰۰ میلیون تومان — فقط یک‌چهارم را نقد بپردازید و باقی را در ۵ قسط ماهانه، بدون سود. بالا رفتن قیمت طلا روی اقساط شما اثری ندارد."));
    }
}
