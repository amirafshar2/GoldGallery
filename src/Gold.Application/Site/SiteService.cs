using System.Text.Json;
using System.Text.RegularExpressions;
using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Site;

public sealed record OnboardingStep(int Order, string Key, string Title, string Description, string AdminLink, bool Done);
public sealed record OnboardingDto(IReadOnlyList<OnboardingStep> Steps, int Percent, OnboardingStep? Next, bool Dismissed, bool Completed);
public sealed record SetupStatusDto(bool NeedsSetup);
public sealed record SetupInitRequest(string SetupToken, string Phone, string FullName);

public sealed partial class SiteService(IAppDbContext db, IClock clock, ICurrentUser user, ISecretHasher hasher)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")] private static partial Regex HexColor();

    public async Task<SiteDto> GetAsync(CancellationToken ct)
    {
        var all = await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Json, ct);
        T Get<T>(string key, Func<T> empty) where T : class =>
            all.TryGetValue(key, out var j) ? JsonSerializer.Deserialize<T>(j, Json) ?? empty() : empty();

        var home = Get(SiteKeys.Home, () => new HomeSection());
        home = home with
        {
            Hero = home.Hero ?? new HeroSection(Images: []),
            Blocks = home.Blocks is { Count: > 0 } ? home.Blocks.OrderBy(b => b.Order).ToList() : HomeSection.DefaultBlocks,
        };
        var trust = Get(SiteKeys.Trust, () => new TrustSection());
        var footer = Get(SiteKeys.Footer, () => new FooterSection());
        var texts = Get(SiteKeys.Texts, () => new TextsSection());
        var owner = Get(SiteKeys.Owner, () => new OwnerSection());

        return new SiteDto(
            Get(SiteKeys.Brand, () => new BrandSection()), Get(SiteKeys.Theme, () => new ThemeSection()), Get(SiteKeys.Contact, () => new ContactSection()),
            Get(SiteKeys.Legal, () => new LegalSection()), home, trust with { Badges = trust.Badges ?? [] },
            footer with { Links = footer.Links ?? [] }, Get(SiteKeys.Seo, () => new SeoSection()), Get(SiteKeys.Features, () => new FeaturesSection()),
            texts with { Values = texts.Values ?? new Dictionary<string, string>() }, Get(SiteKeys.Publish, () => new PublishSection()),
            owner with { Highlights = owner.Highlights ?? [], Reels = owner.Reels ?? [], Tips = owner.Tips ?? [] },
            Get(SiteKeys.Installments, () => new InstallmentsSection()));
    }

    /// <summary>Replace one section. The JSON is validated by deserializing into the section's record type.</summary>
    public async Task<Result<Unit>> SetSectionAsync(string key, JsonElement body, CancellationToken ct)
    {
        if (!SiteKeys.Editable.TryGetValue(key, out var type)) return Error.NotFound("site.section", "این بخش وجود ندارد.");
        object? value;
        try { value = body.Deserialize(type, Json); }
        catch (JsonException) { return Error.Validation("site.json", "ساختار داده‌ی این بخش نامعتبر است."); }
        if (value is null) return Error.Validation("site.json", "بدنه‌ی خالی است.");

        if (value is ThemeSection t &&
            new[] { t.Background, t.Surface, t.Text, t.Primary, t.Muted }.Any(c => !HexColor().IsMatch(c)))
            return Error.Validation("site.theme", "رنگ‌ها باید به‌صورت #RRGGBB باشند.");
        if (value is InstallmentsSection i &&
            (i.DownPercent is < 0 or > 100 || i.Months is < 1 or > 36 || i.MonthlyFeePercent is < 0 or > 10 || i.MaxRial < 0))
            return Error.Validation("site.installments", "پیش‌پرداخت ۰ تا ۱۰۰٪، تعداد قسط ۱ تا ۳۶ و کارمزد ماهانه ۰ تا ۱۰٪ باشد.");
        if (Serialize(value).Length > 200_000) return Error.Validation("site.size", "حجم محتوا بیش از حد مجاز است.");

        var urls = ExtractUrls(value);
        if (urls.Any(u => !IsSafeUrl(u))) return Error.Validation("site.url", "فقط آدرس‌های http(s) یا مسیرهای داخلی مجاز هستند.");

        var json = Serialize(value);
        var row = await db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null) db.SiteSettings.Add(SiteSetting.Create(key, json, clock.Now, user.Id));
        else row.Update(json, clock.Now, user.Id);
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, "site.update", "SiteSetting", key, "", user.IsDemo));
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<OnboardingDto> GetOnboardingAsync(CancellationToken ct)
    {
        var site = await GetAsync(ct);
        var themeSaved = await db.SiteSettings.AnyAsync(s => s.Key == SiteKeys.Theme, ct);
        var prefs = await db.SiteSettings.AsNoTracking().Where(s => s.Key == SiteKeys.Onboarding).Select(s => s.Json).FirstOrDefaultAsync(ct);
        var dismissed = prefs is not null && (JsonSerializer.Deserialize<OnboardingPrefs>(prefs, Json)?.Dismissed ?? false);

        var steps = new List<OnboardingStep>
        {
            new(1, "brand", "نام و لوگو", "نام فروشگاه، شعار و لوگو را وارد کنید تا بالای سایت نمایش داده شود.", "/admin/site/brand",
                site.Brand.Name.Length > 0 && site.Brand.LogoUrl.Length > 0),
            new(2, "theme", "رنگ‌ها و فونت", "رنگ اصلی برند و حالت روشن/تاریک را انتخاب کنید.", "/admin/site/theme", themeSaved),
            new(3, "contact", "تماس و آدرس", "شماره تماس، آدرس مغازه، ساعات کاری و اینستاگرام.", "/admin/site/contact",
                (site.Contact.Phone.Length > 0 || site.Contact.Mobile.Length > 0) && site.Contact.Address.Length > 0),
            new(4, "pricing", "نرخ طلا و اجرت", "نرخ امروز هر گرم ۱۸ عیار، درصد سود و مالیات را ثبت کنید.", "/admin/pricing",
                await db.GoldRates.AnyAsync(ct) && await db.PricingSettings.AnyAsync(ct)),
            new(5, "categories", "دسته‌بندی‌ها", "حداقل یک دسته بسازید (مثلاً انگشتر، دستبند).", "/admin/categories", await db.Categories.AnyAsync(ct)),
            new(6, "branch", "شعبه", "مغازه یا شعبه‌ای که قطعه‌ها در آن موجود است.", "/admin/branches", await db.Branches.AnyAsync(ct)),
            new(7, "product", "اولین محصول و قطعه", "یک محصول بسازید و یک قطعه با وزن واقعی ترازو اضافه کنید.", "/admin/products",
                await db.Pieces.AnyAsync(ct)),
            new(8, "home", "صفحه‌ی اول", "عنوان، متن و عکس‌های اسکرولی صفحه‌ی اول را بگذارید.", "/admin/site/home",
                (site.Home.Hero?.Title.Length ?? 0) > 0 && (site.Home.Hero?.Images?.Count ?? 0) > 0),
            new(9, "publish", "انتشار سایت", "همه‌چیز آماده است؟ سایت را برای مشتری‌ها باز کنید.", "/admin/site/publish", site.Publish.Published),
        };
        var done = steps.Count(s => s.Done);
        return new OnboardingDto(steps, done * 100 / steps.Count, steps.FirstOrDefault(s => !s.Done), dismissed, done == steps.Count);
    }

    // ---------- first-run setup (empty install) ----------

    public async Task<SetupStatusDto> GetSetupStatusAsync(CancellationToken ct) =>
        new(!await db.Users.AnyAsync(u => u.Role == UserRole.Admin && !u.IsDemo, ct));

    public async Task<Result<Unit>> InitAsync(SetupInitRequest req, string? configuredToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(configuredToken) || configuredToken.Length < 16)
            return Error.Unavailable("setup.disabled", "راه‌اندازی اولیه فعال نیست (Setup:Token تنظیم نشده).");
        if (!hasher.Verify(req.SetupToken ?? "", hasher.Hash(configuredToken)))
            return Error.Forbidden("setup.token", "کد نصب اشتباه است.");
        if (!(await GetSetupStatusAsync(ct)).NeedsSetup)
            return Error.Conflict("setup.done", "این سایت قبلاً راه‌اندازی شده است.");

        var phone = Auth.AuthService.NormalizePhone(req.Phone);
        if (!Auth.AuthService.IranMobile().IsMatch(phone)) return Error.Validation("phone.invalid", "شماره موبایل معتبر نیست.");

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);
        if (existing is null) db.Users.Add(User.Create(phone, UserRole.Admin, req.FullName));
        else { existing.ChangeRole(UserRole.Admin); existing.Rename(req.FullName); }
        db.AuditLogs.Add(AuditLog.Create(clock.Now, null, "setup.init", "User", phone[..4] + "***"));
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static string Serialize(object v) => JsonSerializer.Serialize(v, v.GetType(), Json);

    private static IEnumerable<string> ExtractUrls(object v)
    {
        using var doc = JsonDocument.Parse(Serialize(v));
        return Walk(doc.RootElement).ToList();

        static IEnumerable<string> Walk(JsonElement e, string? name = null)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var p in e.EnumerateObject())
                        foreach (var u in Walk(p.Value, p.Name)) yield return u;
                    break;
                case JsonValueKind.Array:
                    foreach (var i in e.EnumerateArray())
                        foreach (var u in Walk(i, name)) yield return u;
                    break;
                case JsonValueKind.String when name is not null &&
                    (name.EndsWith("Url", StringComparison.OrdinalIgnoreCase) || name is "images" or "url" or "ctaLink"):
                    var s = e.GetString();
                    if (!string.IsNullOrEmpty(s)) yield return s;
                    break;
            }
        }
    }

    private static bool IsSafeUrl(string u) =>
        (u.StartsWith('/') && !u.StartsWith("//")) ||
        (Uri.TryCreate(u, UriKind.Absolute, out var x) && (x.Scheme == Uri.UriSchemeHttps || x.Scheme == Uri.UriSchemeHttp));
}
