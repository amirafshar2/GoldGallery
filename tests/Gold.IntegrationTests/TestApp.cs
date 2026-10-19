using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Gold.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Gold.IntegrationTests;

/// <summary>Boots the real API (real SQLite file, real EF Core, real auth) in the "Testing" environment.</summary>
public sealed class TestApp : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"gold-test-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, string?> _settings;

    public TestApp(Dictionary<string, string?>? overrides = null)
    {
        _settings = new()
        {
            ["ConnectionStrings:Default"] = $"Data Source={_db}",
            ["Database:Provider"] = "Sqlite",
            ["Jwt:Key"] = "TEST-key-0123456789abcdef-0123456789abcdef",
            ["Security:HashKey"] = "TEST-hash-0123456789abcdef-0123456789abcdef",
            ["DemoLogin:Enabled"] = "true",
            ["Seed:Mode"] = "Demo",
            ["Setup:Token"] = "TEST-setup-token-123456",
            ["RateGuard:MaxAgeMinutes"] = "1440",
            ["RateLimits:Otp"] = "1000", ["RateLimits:Auth"] = "1000", ["RateLimits:Payment"] = "1000", ["RateLimits:Global"] = "100000",
            ["Media:Root"] = Path.Combine(Path.GetTempPath(), "gold-test-media"),
        };
        if (overrides is not null) foreach (var (k, v) in overrides) _settings[k] = v;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_settings.TryGetValue("Environment", out var env) && env is not null ? env : "Testing");
        foreach (var (k, v) in _settings) builder.UseSetting(k, v);
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public async Task<HttpClient> DemoAsync(string role)
    {
        var c = Client();
        var res = await c.PostAsJsonAsync("/api/v1/auth/demo-login", new { role });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return c;
    }

    /// <summary>Signs in a brand-new customer through the real OTP flow (code read from the dev SMS sender).</summary>
    public async Task<HttpClient> CustomerAsync(string phone)
    {
        var c = Client();
        (await c.PostAsJsonAsync("/api/v1/auth/otp/request", new { phone })).EnsureSuccessStatusCode();
        var code = new string(LogSmsSender.LastMessages[phone].Where(char.IsDigit).ToArray());
        var res = await c.PostAsJsonAsync("/api/v1/auth/otp/verify", new { phone, code });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return c;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _db, _db + "-wal", _db + "-shm" }) try { File.Delete(f); } catch { /* best effort */ }
    }
}

public static class Http
{
    public static async Task<JsonElement> Json(this HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<string> FirstAvailablePieceAsync(this HttpClient c, string slug = "ring-frequency", int index = 0)
    {
        var p = await (await c.GetAsync($"/api/v1/products/{slug}")).Json();
        return p.GetProperty("pieces").EnumerateArray().Where(x => x.GetProperty("status").GetString() == "Available")
            .ElementAt(index).GetProperty("pieceId").GetString()!;
    }

    public static async Task<HttpResponseMessage> OrderPieceAsync(this HttpClient c, string pieceId)
    {
        var q = await c.PostAsJsonAsync("/api/v1/quotes", new { pieceIds = new[] { pieceId } });
        if (!q.IsSuccessStatusCode) return q;
        var qid = (await q.Json()).GetProperty("id").GetString();
        return await c.PostAsJsonAsync("/api/v1/orders", new { quoteId = qid });
    }

    public static async Task<(string PaymentId, string Token)> StartPaymentAsync(this HttpClient c, string orderId, string key)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/{orderId}/payments");
        req.Headers.Add("Idempotency-Key", key);
        var res = await c.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var j = await res.Json();
        var url = j.GetProperty("redirectUrl").GetString()!;
        return (j.GetProperty("paymentId").GetString()!, url[(url.IndexOf("token=") + 6)..]);
    }
}
