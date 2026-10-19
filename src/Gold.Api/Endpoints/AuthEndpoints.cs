using Gold.Api.Infrastructure;
using Gold.Api.Security;
using Gold.Application.Abstractions;
using Gold.Application.Auth;

namespace Gold.Api.Endpoints;

public static class AuthEndpoints
{
    public const string RefreshCookie = "gold_rt";
    /// <summary>Non-secret hint ("1") readable by JS, so the site only tries to refresh when a session can exist.</summary>
    public const string SessionHintCookie = "gold_session";

    public static RouteGroupBuilder MapAuth(this RouteGroupBuilder api, bool demoLoginEnabled)
    {
        var g = api.MapGroup("/auth").WithTags("Auth");

        g.MapPost("/otp/request", async (RequestOtpRequest req, AuthService svc, CancellationToken ct) =>
                (await svc.RequestOtpAsync(req, ct)).ToHttp(_ => Results.NoContent()))
            .Validate<RequestOtpRequest>().RequireRateLimiting("otp");

        g.MapPost("/otp/verify", async (VerifyOtpRequest req, AuthService svc, HttpContext http, CancellationToken ct) =>
                (await svc.VerifyOtpAsync(req, ct)).ToHttp(r => Issue(http, r)))
            .Validate<VerifyOtpRequest>().RequireRateLimiting("auth");

        g.MapPost("/refresh", async (AuthService svc, HttpContext http, CancellationToken ct) =>
            {
                var r = await svc.RefreshAsync(http.Request.Cookies[RefreshCookie], ct);
                if (!r.IsSuccess) http.Response.Cookies.Delete(SessionHintCookie, new CookieOptions { Path = "/" });
                return r.ToHttp(x => Issue(http, x));
            })
            .RequireRateLimiting("auth");

        g.MapPost("/logout", async (AuthService svc, HttpContext http, CancellationToken ct) =>
        {
            await svc.LogoutAsync(http.Request.Cookies[RefreshCookie], ct);
            http.Response.Cookies.Delete(RefreshCookie, CookieOptions(http, DateTimeOffset.UnixEpoch));
            http.Response.Cookies.Delete(SessionHintCookie, new CookieOptions { Path = "/" });
            return Results.NoContent();
        });

        g.MapGet("/me", async (ICurrentUser user, AuthService svc, CancellationToken ct) =>
                (await svc.MeAsync(user.Id!.Value, ct)).ToHttp())
            .RequireAuthorization(Policies.Customer);

        // One-click demo login: endpoint does not even exist unless the feature flag is on (404 otherwise).
        if (demoLoginEnabled)
        {
            g.MapPost("/demo-login", async (DemoLoginRequest req, AuthService svc, HttpContext http, CancellationToken ct) =>
                    (await svc.DemoLoginAsync(req, ct)).ToHttp(r => Issue(http, r)))
                .RequireRateLimiting("auth");
            g.MapGet("/demo-roles", async (AuthService svc, CancellationToken ct) => Results.Ok(await svc.AvailableDemoRolesAsync(ct)));
        }
        return api;
    }

    private static IResult Issue(HttpContext http, AuthResult r)
    {
        http.Response.Cookies.Append(RefreshCookie, r.RefreshToken, CookieOptions(http, r.RefreshExpiresAt));
        http.Response.Cookies.Append(SessionHintCookie, "1", new CookieOptions
        {
            HttpOnly = false, Secure = true, SameSite = SameSiteMode.Strict, Path = "/", Expires = r.RefreshExpiresAt,
        });
        // Refresh token never goes to JavaScript — only in the HttpOnly cookie.
        return Results.Ok(new { user = r.User, accessToken = r.AccessToken, accessExpiresAt = r.AccessExpiresAt });
    }

    private static CookieOptions CookieOptions(HttpContext http, DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        Expires = expires,
        IsEssential = true,
    };
}
