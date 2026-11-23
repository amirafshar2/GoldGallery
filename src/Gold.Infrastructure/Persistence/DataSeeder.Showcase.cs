using System.Text.Json;
using Gold.Application.Site;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;

namespace Gold.Infrastructure.Persistence;

/// <summary>
/// Showcase profile: "Goldatelier Lindner", a fictional jeweller in Stuttgart. Shop, owner, address and phone numbers
/// are invented. Photos are AI images (Canva); prices are computed from the sample rate. Everything is editable in the admin panel.
/// </summary>
public sealed partial class DataSeeder
{
    private void SeedShowcase(DateTimeOffset now)
    {
        var cats = new Dictionary<string, Category>
        {
            ["rings"] = Category.Create("rings", "Ringe", 1),
            ["bracelets"] = Category.Create("bracelets", "Armbänder und Armreifen", 2),
            ["necklaces"] = Category.Create("necklaces", "Ketten und Anhänger", 3),
            ["earrings"] = Category.Create("earrings", "Ohrringe", 4),
            ["sets"] = Category.Create("sets", "Braut-Sets", 5),
            ["men"] = Category.Create("men", "Für Herren", 6),
            ["watches"] = Category.Create("watches", "Golduhren", 7),
            ["invest"] = Category.Create("invest", "Barren und Münzen", 8),
        };
        db.Categories.AddRange(cats.Values);

        var shop = Branch.Create("Goldatelier Lindner", "Musterstraße 21, 70173 Stuttgart");
        db.Branches.Add(shop);

        // (slug, name, category, audience, color, wage %, featured, karat, grams, sizes, image, description)
        var k18 = Karat.K18;
        var products = new (string Slug, string Name, string Cat, Audience A, GoldColor C, int Wage, bool Featured, Karat K, decimal[] Grams, string[] Sizes, string Img, string Desc)[]
        {
            ("clover-bracelet", "Kleeblatt-Armband", "bracelets", Audience.Women, GoldColor.Yellow, 12, true, k18, [15.51m], ["17cm"], "mj-clover",
                "Kleeblatt-Armband mit edler Wirkung; passend zum Kleeblatt-Ring."),
            ("clover-ring", "Kleeblatt-Ring", "rings", Audience.Women, GoldColor.Yellow, 12, false, k18, [5.90m], ["56"], "mj-clover",
                "Kleeblatt-Ring 🍀 – passend zum Kleeblatt-Armband."),
            ("ring-x", "Kreuzring „X“", "rings", Audience.Women, GoldColor.Yellow, 15, false, k18, [3.47m], ["54"], "mj-ring-x",
                "Minimalistisch, aber mit Wirkung – für einen Stil, der schlicht ist und trotzdem auffällt."),
            ("ring-solitaire", "Solitärring", "rings", Audience.Women, GoldColor.Yellow, 18, true, k18, [2.85m, 3.10m], ["53", "56"], "mj-ring-solitaire",
                "Liebe, auf das Wesentliche reduziert. Manchmal sagt ein einziger Stein alles."),
            ("earring-nili", "Ohrhänger Saphirblau", "earrings", Audience.Women, GoldColor.Yellow, 16, true, k18, [7.79m], [""], "mj-earring-nili",
                "Ohrhänger mit blauem Stein – farbig, besonders und ein schönes Geschenk."),
            ("earring-butterfly", "Schmetterlings-Ohrringe", "earrings", Audience.Women, GoldColor.Yellow, 16, false, k18, [4.95m], [""], "mj-earring-butterfly",
                "Ohrringe in Schmetterlingsform mit feinen, durchbrochenen Flügeln 🦋"),
            ("pendant-butterfly", "Schmetterlings-Anhänger", "necklaces", Audience.Women, GoldColor.Yellow, 17, false, k18, [1.05m, 1.20m], ["", ""], "pendant-butterfly",
                "Ein leichter Anhänger – ideal, um mit Gold anzufangen, auch mit kleinem Budget."),
            ("necklace-name", "Namenskette nach Wunsch", "necklaces", Audience.Women, GoldColor.Yellow, 20, false, k18, [2.10m, 2.65m], ["45cm", "50cm"], "necklace-name",
                "Ihr Wunschname in schöner Schrift; bestellen Sie per WhatsApp oder Instagram."),
            ("set-twotone", "Set aus Weiß- und Gelbgold", "sets", Audience.Women, GoldColor.Yellow, 14, true, k18, [52.66m], ["Komplettes Set"], "mj-set-twotone",
                "Kette 32,32 g + Armband 15,34 g + Ohrringe 5 g – eine schöne Kombination aus Weiß- und Gelbgold."),
            ("set-tiffany", "Tiffany-Set mit Originalverschluss", "sets", Audience.Women, GoldColor.White, 15, false, k18, [18.40m], ["Set"], "necklace-tiffany",
                "Elegantes Tiffany-Set mit Originalverschluss."),
            ("halfset-bridal", "Leichtes Braut-Halbset", "sets", Audience.Women, GoldColor.Yellow, 16, true, k18, [14.20m], ["Halbset"], "mj-halfset-bridal",
                "Unsere Halbsets: viel Wirkung bei angenehmem Gewicht 👰🏻‍♀️"),
            ("set-bridal-royal", "Braut-Set „Royal“", "sets", Audience.Women, GoldColor.Yellow, 18, false, k18, [42.50m], ["Komplettes Set"], "set-bridal-royal",
                "Komplettes Braut-Set aus Kette, Ohrringen und Armband."),
            ("watch-women", "Damen-Golduhr", "watches", Audience.Women, GoldColor.Yellow, 10, true, k18, [39.44m], [""], "mj-watches",
                "Luxury Couple Watches – ein edles Statement am Handgelenk. ⌚️"),
            ("watch-men", "Herren-Golduhr", "watches", Audience.Men, GoldColor.Yellow, 10, false, k18, [67.70m], [""], "mj-watches",
                "Für alle, die ihre Zeit nicht nur verbringen, sondern gestalten."),
            ("bracelet-men-cuban", "Herren-Panzerarmband", "men", Audience.Men, GoldColor.Yellow, 8, false, k18, [12.40m, 14.10m], ["21cm", "22cm"], "bracelet-men-cuban",
                "Herren-Armband mit großen Gliedern."),
            ("ring-men-signet", "Herrenring", "men", Audience.Men, GoldColor.Yellow, 10, false, k18, [6.20m], ["64"], "ring-men-signet",
                "Glatter, schwerer Herrenring."),
            ("gold-bar-minted", "Goldbarren 999,9 (geprägt)", "invest", Audience.Unisex, GoldColor.Yellow, 0, true, Karat.Bullion999, [31.103m, 50m, 100m], ["1 Unze", "50 g", "100 g"], "mj-abshodeh",
                "Klassisches Anlagegold ohne Arbeitslohn. Kaufen Sie nur beim zertifizierten Händler."),
            ("maple-leaf", "Maple Leaf Goldmünze", "invest", Audience.Unisex, GoldColor.Yellow, 3, false, Karat.Bullion999, [3.11m, 7.776m, 15.552m], ["1/10 Unze", "1/4 Unze", "1/2 Unze"], "coin-parsian-1g",
                "Beliebter Einstieg in Anlagegold mit kleineren Beträgen."),
            ("gold-bar-cast", "Goldbarren 995 (gegossen)", "invest", Audience.Unisex, GoldColor.Yellow, 1, false, Karat.Bullion995, [5m, 10m], ["5 g", "10 g"], "mj-bullion",
                "Standardbarren für die langfristige Geldanlage."),
            ("bangle-classic", "Armreif mit Diamantschliff", "bracelets", Audience.Women, GoldColor.Yellow, 9, false, k18, [9.12m, 10.30m], ["62", "64"], "bangle-classic",
                "Klassischer Armreif mit Diamantschliff."),
        };

        var barcode = 500_000;
        foreach (var p in products)
        {
            var prod = Product.Create(p.Slug, p.Name, cats[p.Cat].Id, p.A, p.C,
                WageRule.PercentOfRaw(Percentage.FromPercent(p.Wage)), p.Desc, $"/img/p/{p.Img}.jpg", p.Featured);
            db.Products.Add(prod);
            for (var i = 0; i < p.Grams.Length; i++)
            {
                var stone = p.Slug == "ring-solitaire" ? Money.FromRial(95_000) : Money.Zero; // 950 €
                db.Pieces.Add(Piece.Create(prod.Id, shop.Id, $"M{barcode++}", Weight.FromGrams(p.Grams[i]), p.K,
                    p.Sizes[i], stone.Rial > 0 ? Weight.FromGrams(0.10m) : Weight.Zero, stone));
            }
        }

        void Put(string key, object value) =>
            db.SiteSettings.Add(SiteSetting.Create(key, JsonSerializer.Serialize(value, value.GetType(), SiteService.Json), now, null));

        const string address = "Musterstraße 21, 70173 Stuttgart";
        Put(SiteKeys.Brand, new BrandSection("Goldatelier Lindner", "Schmuck · Münzen · Barren · Ratenkauf – Stuttgart", ""));
        Put(SiteKeys.Theme, new ThemeSection(Primary: "#8C6A2E"));
        Put(SiteKeys.Contact, new ContactSection(Phone: "0711 000000", Mobile: "015100000000", WhatsApp: "015100000000",
            Instagram: "goldatelier.demo", Address: address, City: "Stuttgart", WorkingHours: "Mo–Fr 10–19 Uhr, Sa 10–16 Uhr",
            MapUrl: "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString("Stuttgart Königstraße")));
        Put(SiteKeys.Legal, new LegalSection(AboutText:
            "Das Goldatelier Lindner in Stuttgart wird von Markus Lindner geführt, der seit über 10 Jahren mit Gold, Münzen und Edelmetallen arbeitet. " +
            "Wir kaufen und verkaufen Goldbarren, Münzen, Damen- und Herrenschmuck und Braut-Sets – auf Wunsch auch auf Raten. Unser Wert liegt nicht in Zahlen, sondern in Ihrem Vertrauen."));
        Put(SiteKeys.Home, new HomeSection(
            new HeroSection("Goldatelier Lindner – Werte, die glänzen",
                "Goldbarren, Münzen und Schmuck kaufen und verkaufen – mit persönlicher Beratung und zinsfreiem Ratenkauf", "Zur Vitrine", "/shop",
                ["/img/hero/mj-boutique.jpg", "/img/hero/hand-rings.jpg", "/img/hero/neck-necklace.jpg", "/img/hero/boutique.jpg",
                 "/img/hero/goldsmith.jpg", "/img/hero/rose-ring.jpg", "/img/hero/chain-macro.jpg"]),
            [
                new("rate-ticker", true, 1, "Goldpreis aktuell"),
                new("categories", true, 2, "Unsere Vitrine"),
                new("owner", true, 3, "Markus Lindner, Ihr Berater", "Vor dem Kauf mit jemandem sprechen, der den Markt kennt"),
                new("installments", true, 4, "Gold kaufen, in Raten zahlen"),
                new("new-arrivals", true, 5, "Neu in der Vitrine"),
                new("gallery", true, 6, "Auswahl aus dem Atelier", "Durch die Vitrine scrollen"),
                new("story", true, 7, "Unser Wert liegt nicht in Zahlen, sondern in Ihrem Vertrauen", "„Unser Kapital sind Sie.“ Jedes Stück mit echtem Gewicht, garantiertem Feingehalt und offenem Arbeitslohn; der Preis entsteht aus dem Tageskurs."),
                new("buy-back", true, 8, "Altgold oder Barren verkaufen"),
                new("custom-order", true, 9, "Namenskette und Anfertigung"),
                new("trust", true, 10, "Warum Goldatelier Lindner?"),
                new("testimonials", false, 11, "Ihre Meinung"),
                new("contact", true, 12, "Besuchen Sie uns"),
            ]));
        Put(SiteKeys.Trust, new TrustSection(
        [
            new("shield", "Zertifizierter Edelmetallhändler", "Barren und Münzen aus geprüfter Herkunft"),
            new("receipt", "Zinsfreie Raten", "Ein Viertel sofort, der Rest in 5 Monatsraten"),
            new("truck", "Online bestellen, schnell geliefert", "Sichere Zahlung und versicherter Versand"),
            new("sparkles", "Kostenloser Reinigungs-Service", "Für jedes Schmuckstück, das Sie bei uns kaufen"),
        ]));
        Put(SiteKeys.Footer, new FooterSection("Goldatelier Lindner – " + address + ".",
            [new("Ratenkauf", "/installments"), new("Goldpreis heute", "/price"), new("Kontakt", "/contact")]));
        Put(SiteKeys.Seo, new SeoSection("Goldatelier Lindner | Schmuck, Münzen, Barren und Ratenkauf in Stuttgart",
            "Goldbarren, Münzen und Schmuck kaufen und verkaufen – mit tagesaktuellem Preis und zinsfreiem Ratenkauf. Goldatelier Lindner, Stuttgart."));
        Put(SiteKeys.Features, new FeaturesSection(Installments: true));
        Put(SiteKeys.Publish, new PublishSection(true));
        Put(SiteKeys.Owner, new OwnerSection(
            Enabled: true,
            Name: "Markus Lindner",
            Role: "Inhaber, Berater für Goldkauf und Geldanlage",
            PhotoUrl: "/img/hero/goldsmith.jpg",
            Quote: "Legen Sie Ihr Geld mit gutem Gefühl an. Unser Wert liegt nicht in Zahlen, sondern in Ihrem Vertrauen.",
            Highlights: ["Über 10 Jahre Erfahrung am Markt", "Spezialisiert auf Gold, Münzen und Edelmetalle", "Persönliche Beratung für klugen Kauf und Verkauf"],
            Instagram: "",
            Followers: "",
            Reels: [],
            Tips:
            [
                new("Sofort oder auf Termin verkaufen?", "Kennen Sie vor dem Verkauf den Unterschied zwischen sofortiger Auszahlung und Abwicklung auf Termin – dann entscheiden Sie sicherer."),
                new("Gleiches Gewicht, anderer Preis – warum?", "Der Unterschied liegt im Arbeitslohn: Besonderes Design und feinere Verarbeitung kosten mehr."),
                new("Barren als Geldanlage", "Goldbarren haben keinen Arbeitslohn und sind eine kluge Wahl, um den Wert Ihres Geldes zu sichern."),
                new("Steine nicht zum Goldpreis kaufen", "Einen Edelstein zum Goldpreis zu verkaufen ist nicht fair; das Gewicht des Steins muss getrennt berechnet werden."),
                new("Nach und nach kaufen, klug kaufen", "Sie müssen nicht groß anfangen – schon mit kleinen Beträgen geht es los."),
                new("Nur beim zertifizierten Händler kaufen", "Prüfen Sie vor dem Kauf von Barren die Zertifizierung und den Ruf des Händlers."),
            ],
            CtaText: "Kostenlose Beratung mit Herrn Lindner"));
        Put(SiteKeys.Installments, new InstallmentsSection(
            Enabled: true, MaxRial: 1_500_000, DownPercent: 25, Months: 5, MonthlyFeePercent: 0,
            Title: "Gold kaufen, in Raten zahlen",
            Note: "Bis 15.000 € – zahlen Sie nur ein Viertel sofort und den Rest in 5 zinsfreien Monatsraten. Steigt der Goldpreis, bleiben Ihre Raten gleich."));
    }
}
