using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Gold.IntegrationTests;

/// <summary>Full CRUD walk over every admin-managed entity + white-label site content.</summary>
public class CrudAndSiteTests : IDisposable
{
    private readonly TestApp _app = new();
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Catalog_FullCrud_CategoryProductPiece()
    {
        var admin = await _app.DemoAsync("admin");

        // Create
        var cat = await (await admin.PostAsJsonAsync("/api/v1/admin/categories", new { slug = "test-cat", name = "دسته تست", sortOrder = 99 })).Json();
        var catId = cat.GetProperty("id").GetString();
        var branch = await (await admin.PostAsJsonAsync("/api/v1/admin/branches", new { name = "شعبه تست", address = "آدرس" })).Json();
        var branchId = branch.GetProperty("id").GetString();
        var prodRes = await admin.PostAsJsonAsync("/api/v1/admin/products", new
        {
            slug = "test-ring", name = "انگشتر تست", description = "", categoryId = catId, audience = 1, color = 1,
            wageKind = 1, wageValue = 1500, imageUrl = "", isFeatured = false,
        });
        Assert.Equal(HttpStatusCode.Created, prodRes.StatusCode);
        var prodId = (await prodRes.Json()).GetProperty("id").GetString();
        var pieceRes = await admin.PostAsJsonAsync("/api/v1/admin/pieces", new
        {
            productId = prodId, branchId, barcode = "T-1", goldGrams = 2.345m, karat = 750, size = "58", stoneGrams = 0m, stoneValueRial = 0L,
        });
        Assert.Equal(HttpStatusCode.Created, pieceRes.StatusCode);
        var pieceId = (await pieceRes.Json()).GetProperty("id").GetString();

        // Read (public)
        var detail = await (await _app.Client().GetAsync("/api/v1/products/test-ring")).Json();
        Assert.Equal(2.345m, detail.GetProperty("pieces")[0].GetProperty("goldGrams").GetDecimal());

        // Update
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/categories/{catId}", new { slug = "test-cat", name = "دسته ویرایش", sortOrder = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync($"/api/v1/admin/pieces/{pieceId}", new
        {
            productId = prodId, branchId, barcode = "T-1", goldGrams = 2.5m, karat = 750, size = "60", stoneGrams = 0m, stoneValueRial = 0L,
        })).StatusCode);
        detail = await (await _app.Client().GetAsync("/api/v1/products/test-ring")).Json();
        Assert.Equal(2.5m, detail.GetProperty("pieces")[0].GetProperty("goldGrams").GetDecimal());

        // Validation: 4 decimals rejected
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/pieces", new
        {
            productId = prodId, branchId, barcode = "T-2", goldGrams = 1.2345m, karat = 750, size = "", stoneGrams = 0m, stoneValueRial = 0L,
        })).StatusCode);

        // Delete: category with products is protected
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/admin/categories/{catId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/pieces/{pieceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/products/{prodId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _app.Client().GetAsync("/api/v1/products/test-ring")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/categories/{catId}")).StatusCode);
    }

    [Fact]
    public async Task Staff_CanManagePieces_ButNotRates()
    {
        var staff = await _app.DemoAsync("staff");
        Assert.Equal(HttpStatusCode.OK, (await staff.GetAsync("/api/v1/admin/pieces")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PostAsJsonAsync("/api/v1/admin/rates", new { sellPerGram18Rial = 1L, buyPerGram18Rial = 1L })).StatusCode);
    }

    [Fact]
    public async Task OrderStatus_StateMachine_RejectsIllegalTransition()
    {
        var c = await _app.DemoAsync("customer");
        var order = await (await c.OrderPieceAsync(await c.FirstAvailablePieceAsync("earring-hoop"))).Json();
        var staff = await _app.DemoAsync("staff");
        var id = order.GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PutAsJsonAsync($"/api/v1/admin/orders/{id}/status", new { status = 6 })).StatusCode); // → Shipped
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await staff.PutAsJsonAsync($"/api/v1/admin/orders/{id}/status", new { status = 4 })).StatusCode); // → Paid by hand
        Assert.Equal(HttpStatusCode.OK, (await staff.PutAsJsonAsync($"/api/v1/admin/orders/{id}/status", new { status = 9 })).StatusCode); // → Cancelled
    }

    [Fact]
    public async Task Site_EveryTextAndBrandIsEditable_FromAdmin()
    {
        var admin = await _app.DemoAsync("admin");
        var brand = new { name = "طلای امیر", slogan = "شعار جدید", logoUrl = "/media/logo.png", faviconUrl = "" };
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/v1/admin/site/brand", brand)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/v1/admin/site/texts",
            new { values = new Dictionary<string, string> { ["nav.shop"] = "ویترین" } })).StatusCode);

        var site = await (await _app.Client().GetAsync("/api/v1/site")).Json();
        Assert.Equal("طلای امیر", site.GetProperty("brand").GetProperty("name").GetString());
        Assert.Equal("ویترین", site.GetProperty("texts").GetProperty("values").GetProperty("nav.shop").GetString());
    }

    [Fact]
    public async Task Site_RejectsBadColor_AndScriptUrls_AndNonAdmins()
    {
        var admin = await _app.DemoAsync("admin");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync("/api/v1/admin/site/theme", new
        {
            mode = "dark", background = "red", surface = "#000000", text = "#ffffff", primary = "#C9A24A", muted = "#999999", font = "Vazirmatn", radius = "16px",
        })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PutAsJsonAsync("/api/v1/admin/site/brand",
            new { name = "x", slogan = "", logoUrl = "javascript:alert(1)", faviconUrl = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync("/api/v1/admin/site/unknown", new { })).StatusCode);

        var staff = await _app.DemoAsync("staff");
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.PutAsJsonAsync("/api/v1/admin/site/brand", new { name = "x" })).StatusCode);
    }

    [Fact]
    public async Task MediaUpload_AcceptsRealImage_RejectsFakeExtension()
    {
        var admin = await _app.DemoAsync("admin");
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];
        var ok = await admin.PostAsync("/api/v1/admin/media", Form(png, "logo.png"));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var url = (await ok.Json()).GetProperty("url").GetString()!;
        Assert.Equal(HttpStatusCode.OK, (await _app.Client().GetAsync(url)).StatusCode);

        var fake = await admin.PostAsync("/api/v1/admin/media", Form("<script>alert(1)</script>"u8.ToArray(), "evil.png"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fake.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _app.Client().GetAsync("/media/..%2Fappsettings.json")).StatusCode);

        static MultipartFormDataContent Form(byte[] bytes, string name)
        {
            var f = new MultipartFormDataContent();
            f.Add(new ByteArrayContent(bytes), "file", name);
            return f;
        }
    }

    [Fact]
    public async Task BuyBack_Estimate_NoWage()
    {
        var r = await (await _app.Client().PostAsJsonAsync("/api/v1/buyback/estimate", new { mesghal = 1m, karat = 740 })).Json();
        Assert.Equal(4.608m, r.GetProperty("grams").GetDecimal());
        Assert.True(r.GetProperty("valueRial").GetInt64() > 0);
    }
}

/// <summary>An EMPTY install for a new customer: no data, setup wizard, onboarding steps.</summary>
public class EmptyInstallTests : IDisposable
{
    private readonly TestApp _app = new(new() { ["Seed:Mode"] = "Empty" });
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task EmptyInstall_SetupThenOnboardingGuidesStepByStep()
    {
        var anon = _app.Client();
        Assert.True((await (await anon.GetAsync("/api/v1/setup/status")).Json()).GetProperty("needsSetup").GetBoolean());
        Assert.Empty((await (await anon.GetAsync("/api/v1/categories")).Json()).EnumerateArray());
        var site = await (await anon.GetAsync("/api/v1/site")).Json();
        Assert.Equal("", site.GetProperty("brand").GetProperty("name").GetString());
        Assert.False(site.GetProperty("publish").GetProperty("published").GetBoolean());

        // Wrong install code
        Assert.Equal(HttpStatusCode.Forbidden, (await anon.PostAsJsonAsync("/api/v1/setup/init",
            new { setupToken = "nope", phone = "09125556677", fullName = "صاحب مغازه" })).StatusCode);
        // Right install code → first admin
        Assert.Equal(HttpStatusCode.NoContent, (await anon.PostAsJsonAsync("/api/v1/setup/init",
            new { setupToken = "TEST-setup-token-123456", phone = "09125556677", fullName = "صاحب مغازه" })).StatusCode);
        // Second time is refused
        Assert.Equal(HttpStatusCode.Conflict, (await anon.PostAsJsonAsync("/api/v1/setup/init",
            new { setupToken = "TEST-setup-token-123456", phone = "09120000000", fullName = "x" })).StatusCode);

        var admin = await _app.CustomerAsync("09125556677");
        var ob = await (await admin.GetAsync("/api/v1/admin/onboarding")).Json();
        Assert.Equal(0, ob.GetProperty("percent").GetInt32());
        Assert.Equal("brand", ob.GetProperty("next").GetProperty("key").GetString());

        await admin.PutAsJsonAsync("/api/v1/admin/site/brand", new { name = "طلای مشتری", slogan = "", logoUrl = "/media/x.png", faviconUrl = "" });
        ob = await (await admin.GetAsync("/api/v1/admin/onboarding")).Json();
        Assert.Equal("theme", ob.GetProperty("next").GetProperty("key").GetString());
        Assert.True(ob.GetProperty("percent").GetInt32() > 0);

        // Demo login is useless on an empty install (no demo accounts exist)
        Assert.Equal(HttpStatusCode.NotFound, (await anon.PostAsJsonAsync("/api/v1/auth/demo-login", new { role = "admin" })).StatusCode);
    }
}
