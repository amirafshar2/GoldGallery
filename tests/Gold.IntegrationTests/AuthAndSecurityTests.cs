using System.Net;
using System.Net.Http.Json;

namespace Gold.IntegrationTests;

public class AuthAndSecurityTests : IDisposable
{
    private readonly TestApp _app = new();
    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("customer", "Customer")]
    [InlineData("wholesale", "Wholesale")]
    [InlineData("staff", "Staff")]
    [InlineData("admin", "Admin")]
    public async Task DemoLogin_EachRole(string role, string expected)
    {
        var c = await _app.DemoAsync(role);
        var me = await (await c.GetAsync("/api/v1/auth/me")).Json();
        Assert.Equal(expected, me.GetProperty("role").GetString());
        Assert.True(me.GetProperty("isDemo").GetBoolean());
    }

    [Fact]
    public async Task DemoLogin_UnknownRole_Rejected() =>
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await _app.Client().PostAsJsonAsync("/api/v1/auth/demo-login", new { role = "root" })).StatusCode);

    [Fact]
    public async Task RefreshToken_Rotates_AndReuseRevokesFamily()
    {
        var c = _app.Client();
        (await c.PostAsJsonAsync("/api/v1/auth/demo-login", new { role = "customer" })).EnsureSuccessStatusCode();
        var cookie1 = c.DefaultRequestHeaders.Contains("Cookie") ? null : "auto";

        var r1 = await c.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var oldCookie = r1.RequestMessage!.Headers.GetValues("Cookie").First();

        // Replay the OLD (already rotated) cookie from another client → family revoked.
        var thief = _app.Client();
        var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", oldCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await thief.SendAsync(replay)).StatusCode);

        // Even the legitimate latest token is now dead.
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
        _ = cookie1;
    }

    [Fact]
    public async Task WrongOtp_Unauthorized()
    {
        var c = _app.Client();
        await c.PostAsJsonAsync("/api/v1/auth/otp/request", new { phone = "09121112233" });
        var res = await c.PostAsJsonAsync("/api/v1/auth/otp/verify", new { phone = "09121112233", code = "00000" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task InvalidPhone_422() =>
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await _app.Client().PostAsJsonAsync("/api/v1/auth/otp/request", new { phone = "12345" })).StatusCode);

    [Fact]
    public async Task Customer_CannotUseAdminEndpoints()
    {
        var c = await _app.DemoAsync("customer");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/admin/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Client().GetAsync("/api/v1/admin/orders")).StatusCode);
    }

    [Fact]
    public async Task Errors_AreProblemDetails_WithTraceId_NoStackTrace()
    {
        var res = await _app.Client().GetAsync("/api/v1/products/does-not-exist");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("   at ", body);
    }
}

public class DemoLoginDisabledTests : IDisposable
{
    private readonly TestApp _app = new(new() { ["DemoLogin:Enabled"] = "false" });
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task DemoLoginEndpoint_DoesNotExist() =>
        Assert.Equal(HttpStatusCode.NotFound,
            (await _app.Client().PostAsJsonAsync("/api/v1/auth/demo-login", new { role = "admin" })).StatusCode);
}

public class ProductionGuardTests
{
    [Fact]
    public void Production_WithDemoLogin_FailsFast()
    {
        using var app = new TestApp(new() { ["Environment"] = "Production", ["DemoLogin:Enabled"] = "true" });
        var ex = Assert.ThrowsAny<Exception>(() => app.Client());
        Assert.Contains("DemoLogin", ex.ToString());
    }
}
