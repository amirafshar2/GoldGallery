using System.Text.Json;
using Gold.Api.Infrastructure;
using Gold.Api.Security;
using Gold.Application.Site;
using Gold.Infrastructure.Services;

namespace Gold.Api.Endpoints;

public static class SiteEndpoints
{
    public static RouteGroupBuilder MapSite(this RouteGroupBuilder api, IConfiguration config)
    {
        // Public: everything the storefront needs to render brand, theme, texts and home sections.
        var showcase = config.GetValue<bool>("Showcase:Enabled");
        api.MapGet("/site", async (SiteService svc, CancellationToken ct) => Results.Ok((await svc.GetAsync(ct)) with { Showcase = showcase })).WithTags("Site");

        // First-run setup of an EMPTY install: creates the first real admin using the install code.
        var setup = api.MapGroup("/setup").WithTags("Setup");
        setup.MapGet("/status", async (SiteService svc, CancellationToken ct) => Results.Ok(await svc.GetSetupStatusAsync(ct)));
        setup.MapPost("/init", async (SetupInitRequest req, SiteService svc, CancellationToken ct) =>
            (await svc.InitAsync(req, config["Setup:Token"], ct)).ToHttp(_ => Results.NoContent()))
            .RequireRateLimiting("otp");

        var admin = api.MapGroup("/admin").WithTags("Admin site").RequireAuthorization(Policies.Admin);
        admin.MapPut("/site/{section}", async (string section, JsonElement body, SiteService svc, CancellationToken ct) =>
            (await svc.SetSectionAsync(section, body, ct)).ToHttp(_ => Results.NoContent()));
        admin.MapGet("/onboarding", async (SiteService svc, CancellationToken ct) => Results.Ok(await svc.GetOnboardingAsync(ct)));

        api.MapPost("/admin/media", async (IFormFile file, LocalMediaStorage storage, CancellationToken ct) =>
        {
            await using var s = file.OpenReadStream();
            var saved = await storage.SaveAsync(s, file.Length, ct);
            return saved is null
                ? Results.Problem(statusCode: 422, title: "فقط تصویر JPG، PNG، WebP یا AVIF تا ۵ مگابایت، یا ویدیوی MP4 تا ۲۵ مگابایت مجاز است.")
                : Results.Created($"/media/{saved.FileName}", new { url = $"/media/{saved.FileName}", saved.ContentType, saved.Bytes });
        }).WithTags("Admin site").RequireAuthorization(Policies.Staff)
          .DisableAntiforgery(); // JWT bearer auth (no cookies) → not CSRF-exposed

        return api;
    }

    public static void MapMedia(this WebApplication app) =>
        app.MapGet("/media/{name}", (string name, LocalMediaStorage storage, HttpContext http) =>
        {
            var f = storage.Open(name);
            if (f is null) return Results.NotFound();
            http.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(f.Value.Path, f.Value.Type, enableRangeProcessing: true);
        });
}
