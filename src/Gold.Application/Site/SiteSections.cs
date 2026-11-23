namespace Gold.Application.Site;

// Every record has safe empty defaults so a brand-new (empty) install renders without errors.

public sealed record BrandSection(string Name = "", string Slogan = "", string LogoUrl = "", string FaviconUrl = "");

public sealed record ThemeSection(
    string Mode = "light", string Background = "#F6F1E9", string Surface = "#FFFDF9", string Text = "#1D1812",
    string Primary = "#8E6B2F", string Muted = "#6F6556", string Font = "Vazirmatn", string Radius = "18px");

public sealed record ContactSection(
    string Phone = "", string Mobile = "", string WhatsApp = "", string Instagram = "", string Telegram = "",
    string Email = "", string Address = "", string City = "", string WorkingHours = "", string MapUrl = "",
    decimal? Latitude = null, decimal? Longitude = null);

public sealed record LegalSection(string LicenseNumber = "", string EconomicCode = "", string TrustSealUrl = "", string AboutText = "");

public sealed record HeroSection(string Title = "", string Subtitle = "", string CtaText = "", string CtaLink = "/shop", IReadOnlyList<string>? Images = null);

public sealed record HomeBlock(string Key, bool Enabled, int Order, string Title = "", string Subtitle = "");

public sealed record HomeSection(HeroSection? Hero = null, IReadOnlyList<HomeBlock>? Blocks = null)
{
    public static readonly IReadOnlyList<HomeBlock> DefaultBlocks =
    [
        new("rate-ticker", true, 1, "Goldpreis aktuell"),
        new("categories", true, 2, "Kategorien"),
        new("new-arrivals", true, 3, "Neuheiten"),
        new("owner", true, 4, "Ihr Berater", "Vor dem Kauf mit jemandem sprechen, der den Markt kennt"),
        new("gallery", true, 5, "Ausgewählte Kollektion", "Durch die Vitrine scrollen"),
        new("installments", true, 6, "Gold kaufen, in Raten zahlen"),
        new("story", true, 7, "Von Hand gefertigt, auf der Waage geprüft", "Jedes Stück hat ein echtes Gewicht, garantierten Feingehalt und einen offenen Arbeitslohn. Den Preis berechnen wir aus dem Tageskurs, nicht aus dem Bauchgefühl."),
        new("custom-order", true, 8, "Ihr Wunschschmuck nach Maß"),
        new("buy-back", true, 9, "Altgold eintauschen"),
        new("trust", true, 10, "Warum wir?"),
        new("testimonials", false, 11, "Kundenstimmen"),
        new("contact", true, 12, "Geschäft und Anfahrt"),
    ];
}

public sealed record TrustBadge(string Icon, string Title, string Text);

public sealed record TrustSection(IReadOnlyList<TrustBadge>? Badges = null);

public sealed record FooterLink(string Title, string Url);

public sealed record FooterSection(string Text = "", IReadOnlyList<FooterLink>? Links = null);

public sealed record SeoSection(string Title = "", string Description = "", string OgImageUrl = "");

public sealed record FeaturesSection(
    bool Shop = true, bool NameNecklace = true, bool CustomOrder = true, bool BuyBack = true, bool Bridal = true,
    bool Wholesale = false, bool Installments = false, bool Blog = false, bool Repairs = true);

/// <summary>Overrides for any UI label (key → text). The frontend has defaults; admin can change any of them.</summary>
public sealed record TextsSection(IReadOnlyDictionary<string, string>? Values = null);

public sealed record PublishSection(bool Published = false, string MaintenanceMessage = "Wir sind bald für Sie da.");

public sealed record OnboardingPrefs(bool Dismissed = false);

/// <summary>A short video of the owner/advisor: an uploaded MP4 (works everywhere) and/or an Instagram reel link.</summary>
public sealed record OwnerReel(string Title = "", string InstagramUrl = "", string VideoUrl = "", string PosterUrl = "");

public sealed record OwnerTip(string Title = "", string Text = "");

/// <summary>"Meet the owner" — for shops whose owner is the face of the brand (photo, quote, videos, tips).</summary>
public sealed record OwnerSection(
    bool Enabled = false, string Name = "", string Role = "", string PhotoUrl = "", string Quote = "",
    IReadOnlyList<string>? Highlights = null, string Instagram = "", string Followers = "",
    IReadOnlyList<OwnerReel>? Reels = null, IReadOnlyList<OwnerTip>? Tips = null,
    string CtaText = "Kostenlose Beratung per WhatsApp");

/// <summary>Installment purchase terms (e.g. ¼ down payment, rest in N interest-free monthly payments).</summary>
public sealed record InstallmentsSection(
    bool Enabled = false, long MaxRial = 3_000_000_000, int DownPercent = 25, int Months = 5, int MonthlyFeePercent = 0,
    string Title = "Gold auf Raten", string Note = "");

public sealed record SiteDto(
    BrandSection Brand, ThemeSection Theme, ContactSection Contact, LegalSection Legal, HomeSection Home,
    TrustSection Trust, FooterSection Footer, SeoSection Seo, FeaturesSection Features, TextsSection Texts,
    PublishSection Publish, OwnerSection Owner, InstallmentsSection Installments, bool Showcase = false);

public static class SiteKeys
{
    public const string Brand = "brand", Theme = "theme", Contact = "contact", Legal = "legal", Home = "home",
        Trust = "trust", Footer = "footer", Seo = "seo", Features = "features", Texts = "texts",
        Publish = "publish", Onboarding = "onboarding", Owner = "owner", Installments = "installments";

    public static readonly IReadOnlyDictionary<string, Type> Editable = new Dictionary<string, Type>
    {
        [Brand] = typeof(BrandSection), [Theme] = typeof(ThemeSection), [Contact] = typeof(ContactSection),
        [Legal] = typeof(LegalSection), [Home] = typeof(HomeSection), [Trust] = typeof(TrustSection),
        [Footer] = typeof(FooterSection), [Seo] = typeof(SeoSection), [Features] = typeof(FeaturesSection),
        [Texts] = typeof(TextsSection), [Publish] = typeof(PublishSection), [Onboarding] = typeof(OnboardingPrefs),
        [Owner] = typeof(OwnerSection), [Installments] = typeof(InstallmentsSection),
    };
}
