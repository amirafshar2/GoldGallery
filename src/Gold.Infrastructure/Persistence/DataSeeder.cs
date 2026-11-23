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
    public const string DemoCustomerPhone = "015100000001";

    /// <summary>Seed profiles: "Generic" (default sample shop) or "Showcase" (a fictional German jeweller).</summary>
    public async Task SeedAsync(string? profile = null, CancellationToken ct = default)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(ct)) return;
        var now = clock.Now;

        db.Users.AddRange(
            User.Create(DemoCustomerPhone, UserRole.Customer, "Beispielkunde", isDemo: true),
            User.Create("015100000002", UserRole.Wholesale, "Beispiel-Großhandelspartner", isDemo: true),
            User.Create("015100000003", UserRole.Staff, "Beispiel-Verkäufer", isDemo: true),
            User.Create("015100000004", UserRole.Admin, "Beispiel-Admin", isDemo: true));

        // 30 days of rates in euro cents per gram of 750 gold (sell / buy-back), last one is "now".
        var baseCents = 7_980L;
        for (var d = 30; d >= 1; d--)
        {
            var cents = baseCents + (30 - d) * 9 + (d % 4 == 0 ? -25 : 15);
            db.GoldRates.Add(GoldRateEntry.From(GoldRate.Create(Money.FromRial(cents), Money.FromRial(cents - 340), now.AddDays(-d), "seed"), null));
        }
        db.GoldRates.Add(GoldRateEntry.From(GoldRate.Create(Money.FromRial(8_240), Money.FromRial(7_900), now, "seed"), null));
        db.PricingSettings.Add(PricingSettings.Create(PricingPolicy.Demo, now.AddDays(-31), null));

        if (string.Equals(profile, "Showcase", StringComparison.OrdinalIgnoreCase))
        {
            SeedShowcase(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        var cats = new Dictionary<string, Category>
        {
            ["rings"] = Category.Create("rings", "Ringe", 1),
            ["bracelets"] = Category.Create("bracelets", "Armbänder", 2),
            ["bangles"] = Category.Create("bangles", "Armreifen", 3),
            ["necklaces"] = Category.Create("necklaces", "Ketten", 4),
            ["chains"] = Category.Create("chains", "Gliederketten", 5),
            ["earrings"] = Category.Create("earrings", "Ohrringe", 6),
            ["pendants"] = Category.Create("pendants", "Anhänger", 7),
            ["anklets"] = Category.Create("anklets", "Fußkettchen", 8),
            ["sets"] = Category.Create("sets", "Sets", 9),
            ["coins"] = Category.Create("coins", "Münzen und Barren", 10),
        };
        db.Categories.AddRange(cats.Values);

        var b1 = Branch.Create("Hauptgeschäft", "Beispiel – Hauptstraße, Goldpassage");
        var b2 = Branch.Create("Filiale Einkaufszentrum", "Beispiel – Einkaufszentrum, 1. Etage");
        db.Branches.AddRange(b1, b2);

        var products = new (string Slug, string Name, string Cat, Audience A, GoldColor C, int WagePct, bool Featured, decimal[] Grams, string[] Sizes)[]
        {
            ("ring-frequency", "Goldring „Frequenz“", "rings", Audience.Women, GoldColor.Yellow, 14, true, [1.44m, 1.46m, 1.61m], ["56", "58", "60"]),
            ("ring-baguette", "Baguette-Ring mit Steinen", "rings", Audience.Women, GoldColor.White, 18, true, [2.31m, 2.40m], ["54", "57"]),
            ("ring-men-signet", "Herren-Siegelring", "rings", Audience.Men, GoldColor.Yellow, 12, false, [6.20m, 6.85m], ["64", "66"]),
            ("ring-kids-heart", "Kinderring Herz", "rings", Audience.Kids, GoldColor.Rose, 16, false, [0.82m], ["40"]),
            ("bracelet-rope", "Kordelarmband", "bracelets", Audience.Women, GoldColor.Yellow, 9, true, [3.12m, 3.40m, 4.05m], ["17cm", "18cm", "19cm"]),
            ("bracelet-men-cuban", "Herren-Panzerarmband", "bracelets", Audience.Men, GoldColor.Yellow, 8, false, [12.40m, 14.10m], ["21cm", "22cm"]),
            ("bangle-classic", "Klassischer Armreif mit Diamantschliff", "bangles", Audience.Women, GoldColor.Yellow, 7, true, [8.50m, 9.12m, 10.30m], ["62", "64", "66"]),
            ("necklace-name", "Namenskette nach Wunsch", "necklaces", Audience.Women, GoldColor.Yellow, 20, true, [2.10m, 2.65m], ["45cm", "50cm"]),
            ("necklace-tiffany", "Tiffany-Kette mit Solitär", "necklaces", Audience.Women, GoldColor.White, 22, false, [3.30m], ["42cm"]),
            ("chain-venetian", "Venezianerkette", "chains", Audience.Unisex, GoldColor.Yellow, 6, false, [4.20m, 5.10m, 6.00m], ["45cm", "50cm", "55cm"]),
            ("earring-hoop", "Kordel-Creolen", "earrings", Audience.Women, GoldColor.Yellow, 15, true, [1.95m, 2.08m], ["", ""]),
            ("earring-drop", "Tropfen-Ohrringe (hohl)", "earrings", Audience.Women, GoldColor.Rose, 17, false, [1.20m, 1.32m], ["", ""]),
            ("pendant-butterfly", "Schmetterlings-Anhänger (filigran)", "pendants", Audience.Women, GoldColor.Yellow, 19, false, [0.95m, 1.05m], ["", ""]),
            ("anklet-beads", "Kugel-Fußkettchen", "anklets", Audience.Women, GoldColor.Yellow, 13, false, [2.40m], ["24cm"]),
            ("set-bridal-royal", "Braut-Set „Royal“", "sets", Audience.Women, GoldColor.Yellow, 18, true, [42.50m], ["Komplettes Set"]),
            ("coin-parsian-1g", "Maple Leaf Goldmünze 1/10 Unze", "coins", Audience.Unisex, GoldColor.Yellow, 3, false, [3.11m, 3.11m, 3.11m], ["", "", ""]),
        };

        var barcode = 100_000;
        foreach (var p in products)
        {
            var prod = Product.Create(p.Slug, p.Name, cats[p.Cat].Id, p.A, p.C,
                WageRule.PercentOfRaw(Percentage.FromPercent(p.WagePct)),
                $"{p.Name} – 750er Gold mit Punze und ordentlicher Rechnung. (Beispieltext)", $"/img/p/{p.Slug}.jpg", p.Featured);
            db.Products.Add(prod);
            for (var i = 0; i < p.Grams.Length; i++)
            {
                var karat = p.Cat == "coins" ? Karat.Bullion999 : Karat.K18;
                var stone = p.Slug.Contains("baguette") || p.Slug.Contains("tiffany") ? Money.FromRial(45_000) /* 450 € */ : Money.Zero;
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

        Put(SiteKeys.Brand, new BrandSection("Goldgalerie Muster", "750er Gold, transparent und mit ordentlicher Rechnung", ""));
        Put(SiteKeys.Theme, new ThemeSection());
        Put(SiteKeys.Contact, new ContactSection(Phone: "0711 000000", Mobile: "015100000000", Instagram: "sample.gold",
            Address: "Beispiel – Hauptstraße 1, Goldpassage", City: "Musterstadt", WorkingHours: "Mo–Fr 10–19 Uhr, Sa 10–16 Uhr"));
        Put(SiteKeys.Legal, new LegalSection("HRB 000000", "DE000000000", "", "Der Text „Über uns“ ist ein Beispiel und lässt sich im Admin-Panel ändern."));
        Put(SiteKeys.Home, new HomeSection(
            new HeroSection("Goldglanz bei jedem Scrollen", "Transparenter Preis aus Tageskurs, echtem Gewicht und klarem Arbeitslohn", "Zum Shop", "/shop",
                ["/img/hero/liquid-gold.jpg", "/img/hero/hand-rings.jpg", "/img/hero/neck-necklace.jpg", "/img/hero/boutique.jpg",
                 "/img/hero/goldsmith.jpg", "/img/hero/rose-ring.jpg", "/img/hero/chain-macro.jpg"]),
            HomeSection.DefaultBlocks));
        Put(SiteKeys.Trust, new TrustSection(
        [
            new("receipt", "Ordentliche Rechnung", "Gewicht, Feingehalt, Arbeitslohn und Steuer einzeln ausgewiesen"),
            new("refresh", "14 Tage Umtausch", "In allen Filialen mit Rechnung"),
            new("wrench", "Lebenslanger Reparaturservice", "Für Gold, das Sie bei uns gekauft haben"),
            new("truck", "Versicherter Versand", "Deutschlandweit"),
        ]));
        Put(SiteKeys.Footer, new FooterSection("Alle Texte und Bilder auf dieser Website sind Beispiele.", [new("AGB", "/terms"), new("Kontakt", "/contact")]));
        Put(SiteKeys.Seo, new SeoSection("Goldgalerie Muster | Gold online kaufen", "750er Gold zum Tageskurs und mit ordentlicher Rechnung kaufen"));
        Put(SiteKeys.Features, new FeaturesSection());
        Put(SiteKeys.Publish, new PublishSection(true));
    }
}
