using System.Text.Json;
using Gold.Application.Abstractions;
using Gold.Application.Site;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Gold.Infrastructure.Persistence;

/// <summary>Demo data. Every name, price and weight is sample data, editable from the admin panel.</summary>
public sealed partial class DataSeeder(AppDbContext db, IClock clock)
{
    public const string DemoCustomerPhone = "09000000001";

    /// <summary>Seed profiles: "Generic" (default sample shop) or "Moraveji" (showcase for گالری طلای مروجی).</summary>
    public async Task SeedAsync(string? profile = null, CancellationToken ct = default)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(ct)) return;
        var now = clock.Now;

        db.Users.AddRange(
            User.Create(DemoCustomerPhone, UserRole.Customer, "مشتری نمونه", isDemo: true),
            User.Create("09000000002", UserRole.Wholesale, "همکار عمده نمونه", isDemo: true),
            User.Create("09000000003", UserRole.Staff, "فروشنده نمونه", isDemo: true),
            User.Create("09000000004", UserRole.Admin, "مدیر نمونه", isDemo: true));

        // 30 days of rates (toman → rial), last one is "now".
        var baseToman = 25_200_000L;
        for (var d = 30; d >= 1; d--)
        {
            var toman = baseToman + (30 - d) * 23_000 + (d % 4 == 0 ? -60_000 : 40_000);
            db.GoldRates.Add(GoldRateEntry.From(GoldRate.Create(Money.FromToman(toman), Money.FromToman(toman - 350_000), now.AddDays(-d), "seed"), null));
        }
        db.GoldRates.Add(GoldRateEntry.From(GoldRate.Create(Money.FromToman(25_895_000), Money.FromToman(25_545_000), now, "seed"), null));
        db.PricingSettings.Add(PricingSettings.Create(PricingPolicy.Demo, now.AddDays(-31), null));

        if (string.Equals(profile, "Moraveji", StringComparison.OrdinalIgnoreCase))
        {
            SeedMoraveji(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        var cats = new Dictionary<string, Category>
        {
            ["rings"] = Category.Create("rings", "انگشتر", 1),
            ["bracelets"] = Category.Create("bracelets", "دستبند", 2),
            ["bangles"] = Category.Create("bangles", "النگو", 3),
            ["necklaces"] = Category.Create("necklaces", "گردنبند", 4),
            ["chains"] = Category.Create("chains", "زنجیر", 5),
            ["earrings"] = Category.Create("earrings", "گوشواره", 6),
            ["pendants"] = Category.Create("pendants", "آویز و پلاک", 7),
            ["anklets"] = Category.Create("anklets", "پابند", 8),
            ["sets"] = Category.Create("sets", "ست و نیم‌ست", 9),
            ["coins"] = Category.Create("coins", "سکه و شمش", 10),
        };
        db.Categories.AddRange(cats.Values);

        var b1 = Branch.Create("شعبه‌ی مرکزی", "نمونه — خیابان اصلی، پاساژ طلا");
        var b2 = Branch.Create("شعبه‌ی مرکز خرید", "نمونه — مرکز خرید، طبقه‌ی اول");
        db.Branches.AddRange(b1, b2);

        var products = new (string Slug, string Name, string Cat, Audience A, GoldColor C, int WagePct, bool Featured, decimal[] Grams, string[] Sizes)[]
        {
            ("ring-frequency", "انگشتر طلا فرکانس", "rings", Audience.Women, GoldColor.Yellow, 14, true, [1.44m, 1.46m, 1.61m], ["56", "58", "60"]),
            ("ring-baguette", "انگشتر باگت نگین‌دار", "rings", Audience.Women, GoldColor.White, 18, true, [2.31m, 2.40m], ["54", "57"]),
            ("ring-men-signet", "انگشتر مردانه رکاب", "rings", Audience.Men, GoldColor.Yellow, 12, false, [6.20m, 6.85m], ["64", "66"]),
            ("ring-kids-heart", "انگشتر کودک قلب", "rings", Audience.Kids, GoldColor.Rose, 16, false, [0.82m], ["40"]),
            ("bracelet-rope", "دستبند طنابی", "bracelets", Audience.Women, GoldColor.Yellow, 9, true, [3.12m, 3.40m, 4.05m], ["17cm", "18cm", "19cm"]),
            ("bracelet-men-cuban", "دستبند مردانه کارتیه", "bracelets", Audience.Men, GoldColor.Yellow, 8, false, [12.40m, 14.10m], ["21cm", "22cm"]),
            ("bangle-classic", "النگو کلاسیک تراش‌دار", "bangles", Audience.Women, GoldColor.Yellow, 7, true, [8.50m, 9.12m, 10.30m], ["62", "64", "66"]),
            ("necklace-name", "گردنبند اسم سفارشی", "necklaces", Audience.Women, GoldColor.Yellow, 20, true, [2.10m, 2.65m], ["45cm", "50cm"]),
            ("necklace-tiffany", "گردنبند تیفانی تک نگین", "necklaces", Audience.Women, GoldColor.White, 22, false, [3.30m], ["42cm"]),
            ("chain-venetian", "زنجیر ونیزی", "chains", Audience.Unisex, GoldColor.Yellow, 6, false, [4.20m, 5.10m, 6.00m], ["45cm", "50cm", "55cm"]),
            ("earring-hoop", "گوشواره حلقه‌ای طنابی", "earrings", Audience.Women, GoldColor.Yellow, 15, true, [1.95m, 2.08m], ["", ""]),
            ("earring-drop", "گوشواره اشک توخالی", "earrings", Audience.Women, GoldColor.Rose, 17, false, [1.20m, 1.32m], ["", ""]),
            ("pendant-butterfly", "پلاک پروانه خطی", "pendants", Audience.Women, GoldColor.Yellow, 19, false, [0.95m, 1.05m], ["", ""]),
            ("anklet-beads", "پابند گوی", "anklets", Audience.Women, GoldColor.Yellow, 13, false, [2.40m], ["24cm"]),
            ("set-bridal-royal", "سرویس عروس رویال", "sets", Audience.Women, GoldColor.Yellow, 18, true, [42.50m], ["ست کامل"]),
            ("coin-parsian-1g", "سکه پارسیان ۱ گرمی", "coins", Audience.Unisex, GoldColor.Yellow, 3, false, [1.00m, 1.00m, 1.00m], ["", "", ""]),
        };

        var barcode = 100_000;
        foreach (var p in products)
        {
            var prod = Product.Create(p.Slug, p.Name, cats[p.Cat].Id, p.A, p.C,
                WageRule.PercentOfRaw(Percentage.FromPercent(p.WagePct)),
                $"{p.Name} — طلای ۱۸ عیار با انگ و فاکتور رسمی. (توضیحات نمونه)", $"/img/p/{p.Slug}.jpg", p.Featured);
            db.Products.Add(prod);
            for (var i = 0; i < p.Grams.Length; i++)
            {
                var karat = p.Cat == "coins" ? Karat.Coin : Karat.K18;
                var stone = p.Slug.Contains("baguette") || p.Slug.Contains("tiffany") ? Money.FromToman(4_500_000) : Money.Zero;
                db.Pieces.Add(Piece.Create(prod.Id, i % 2 == 0 ? b1.Id : b2.Id, $"G{barcode++}", Weight.FromGrams(p.Grams[i]), karat,
                    p.Sizes[i], stone.Rial > 0 ? Weight.FromGrams(0.12m) : Weight.Zero, stone));
            }
        }

        SeedSite(now);
        await db.SaveChangesAsync(ct);
    }

    private void SeedSite(DateTimeOffset now)
    {
        void Put(string key, object value) =>
            db.SiteSettings.Add(SiteSetting.Create(key, JsonSerializer.Serialize(value, value.GetType(), SiteService.Json), now, null));

        Put(SiteKeys.Brand, new BrandSection("گالری طلای نمونه", "طلای ۱۸ عیار، شفاف و با فاکتور رسمی", ""));
        Put(SiteKeys.Theme, new ThemeSection());
        Put(SiteKeys.Contact, new ContactSection(Phone: "021-00000000", Mobile: "09000000000", Instagram: "sample.gold",
            Address: "نمونه — خیابان اصلی، پاساژ طلا، پلاک ۱", City: "تهران", WorkingHours: "شنبه تا پنجشنبه ۱۰ تا ۲۱"));
        Put(SiteKeys.Legal, new LegalSection("نمونه-۱۲۳۴", "نمونه-۴۱۱۱", "", "متن «درباره‌ی ما» نمونه است و از پنل ادمین تغییر می‌کند."));
        Put(SiteKeys.Home, new HomeSection(
            new HeroSection("درخشش طلا، در هر اسکرول", "قیمت شفاف بر اساس نرخ لحظه‌ای، وزن واقعی ترازو و اجرت مشخص", "مشاهده‌ی فروشگاه", "/shop",
                ["/img/hero/liquid-gold.jpg", "/img/hero/hand-rings.jpg", "/img/hero/neck-necklace.jpg", "/img/hero/boutique.jpg",
                 "/img/hero/goldsmith.jpg", "/img/hero/rose-ring.jpg", "/img/hero/chain-macro.jpg"]),
            HomeSection.DefaultBlocks));
        Put(SiteKeys.Trust, new TrustSection(
        [
            new("receipt", "فاکتور رسمی", "وزن، عیار، اجرت و مالیات جدا در فاکتور"),
            new("refresh", "تعویض ۴ روزه", "در همه‌ی شعبه‌ها با فاکتور"),
            new("wrench", "تعمیرات مادام‌العمر", "برای طلای خریداری‌شده از ما"),
            new("truck", "ارسال بیمه‌شده", "به سراسر کشور"),
        ]));
        Put(SiteKeys.Footer, new FooterSection("تمام متن‌ها و تصاویر این سایت نمونه هستند.", [new("قوانین", "/terms"), new("تماس با ما", "/contact")]));
        Put(SiteKeys.Seo, new SeoSection("گالری طلای نمونه | خرید آنلاین طلا", "خرید طلای ۱۸ عیار با قیمت لحظه‌ای و فاکتور رسمی"));
        Put(SiteKeys.Features, new FeaturesSection());
        Put(SiteKeys.Publish, new PublishSection(true));
    }
}
