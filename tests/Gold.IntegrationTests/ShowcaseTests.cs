using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Gold.IntegrationTests;

/// <summary>Public showcase (demo for presenting the site to a shop) + the Moraveji seed profile.</summary>
public class ShowcaseTests : IDisposable
{
    private readonly TestApp _app = new(new() { ["Showcase:Enabled"] = "true", ["Seed:Profile"] = "Moraveji" });
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Site_FlagsShowcase_AndSeedsShopProfile()
    {
        var site = await _app.Client().GetFromJsonAsync<JsonElement>("/api/v1/site");
        Assert.True(site.GetProperty("showcase").GetBoolean());
        Assert.Equal("گالری طلای مروجی", site.GetProperty("brand").GetProperty("name").GetString());
        Assert.True(site.GetProperty("owner").GetProperty("enabled").GetBoolean());
        var inst = site.GetProperty("installments");
        Assert.Equal(25, inst.GetProperty("downPercent").GetInt32());
        Assert.Equal(5, inst.GetProperty("months").GetInt32());
        var products = await _app.Client().GetFromJsonAsync<JsonElement>("/api/v1/products?pageSize=60");
        Assert.True(products.GetProperty("total").GetInt32() >= 15);
    }

    [Fact]
    public async Task Installments_RejectsImpossibleTerms()
    {
        var admin = await _app.DemoAsync("admin");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync("/api/v1/admin/site/installments",
            new { enabled = true, maxRial = 1000, downPercent = 150, months = 5, monthlyFeePercent = 0, title = "x", note = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/v1/admin/site/installments",
            new { enabled = true, maxRial = 2_000_000_000, downPercent = 30, months = 6, monthlyFeePercent = 0, title = "اقساط", note = "" })).StatusCode);
    }

    [Fact]
    public async Task Owner_RejectsScriptVideoUrl()
    {
        var admin = await _app.DemoAsync("admin");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync("/api/v1/admin/site/owner",
            new { enabled = true, name = "x", reels = new[] { new { title = "v", instagramUrl = "javascript:alert(1)", videoUrl = "", posterUrl = "" } } })).StatusCode);
    }
}

public class ShowcaseProductionTests
{
    [Fact]
    public async Task Production_WithShowcase_AllowsDemoLogin()
    {
        using var app = new TestApp(new() { ["Environment"] = "Production", ["DemoLogin:Enabled"] = "true", ["Showcase:Enabled"] = "true" });
        var r = await app.Client().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}

public class ShowcaseWithoutSecretsTests
{
    [Fact]
    public async Task Showcase_StartsWithoutKeys_ProductionDoesNot()
    {
        using var demo = new TestApp(new() { ["Environment"] = "Production", ["Showcase:Enabled"] = "true", ["DemoLogin:Enabled"] = "true", ["Jwt:Key"] = "", ["Security:HashKey"] = "" });
        Assert.Equal(HttpStatusCode.OK, (await demo.Client().GetAsync("/health/live")).StatusCode);
        var admin = await demo.DemoAsync("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/v1/admin/onboarding")).StatusCode);

        using var prod = new TestApp(new() { ["Environment"] = "Production", ["Jwt:Key"] = "" });
        Assert.ThrowsAny<Exception>(() => prod.Client());
    }
}
