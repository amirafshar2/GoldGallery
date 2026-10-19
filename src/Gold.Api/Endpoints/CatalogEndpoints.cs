using Gold.Api.Infrastructure;
using Gold.Api.Realtime;
using Gold.Api.Security;
using Gold.Application.Catalog;
using Gold.Application.Rates;
using Gold.Domain.Entities;
using Microsoft.Extensions.Caching.Hybrid;

namespace Gold.Api.Endpoints;

public static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalog(this RouteGroupBuilder api)
    {
        // ---- public ----
        var rates = api.MapGroup("/rates").WithTags("Rates");
        rates.MapGet("/current", async (RateService svc, HybridCache cache, CancellationToken ct) =>
        {
            // Cache only the plain DTO (Result<T> is not serializable). Short expiry: health depends on time.
            var dto = await cache.GetOrCreateAsync("rates:current", async c => (await svc.GetCurrentAsync(c)).Value,
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(30), LocalCacheExpiration = TimeSpan.FromSeconds(30) },
                [SignalRRateNotifier.RateTag], ct);
            return dto is null
                ? ResultExtensions.Problem(Gold.Application.Common.Error.Unavailable("rate.missing", "نرخ روز هنوز ثبت نشده است."))
                : Results.Ok(dto);
        });
        rates.MapGet("/history", async (int? days, RateService svc, CancellationToken ct) => Results.Ok(await svc.GetHistoryAsync(days ?? 30, ct)));

        var cat = api.MapGroup("").WithTags("Catalog");
        cat.MapGet("/categories", async (CatalogService svc, CancellationToken ct) => Results.Ok(await svc.GetCategoriesAsync(ct)));
        cat.MapGet("/branches", async (CatalogService svc, CancellationToken ct) => Results.Ok(await svc.GetBranchesAsync(ct)));
        cat.MapGet("/products", async ([AsParameters] ProductListQueryParams p, CatalogService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetProductsAsync(p.ToQuery(), ct)));
        cat.MapGet("/products/{slug}", async (string slug, CatalogService svc, CancellationToken ct) =>
            (await svc.GetProductAsync(slug, ct)).ToHttp());

        // ---- admin / staff ----
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Staff);

        admin.MapPost("/rates", async (SetRateRequest req, RateService svc, CancellationToken ct) =>
            (await svc.SetAsync(req, ct)).ToHttp(r => Results.Created("/api/v1/rates/current", r)))
            .Validate<SetRateRequest>().RequireAuthorization(Policies.Admin);
        admin.MapGet("/pricing-settings", async (RateService svc, CancellationToken ct) => Results.Ok(await svc.GetSettingsAsync(ct)));
        admin.MapPut("/pricing-settings", async (SetPricingSettingsRequest req, RateService svc, CancellationToken ct) =>
            (await svc.SetSettingsAsync(req, ct)).ToHttp()).Validate<SetPricingSettingsRequest>().RequireAuthorization(Policies.Admin);

        admin.MapPost("/categories", async (UpsertCategoryRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.CreateCategoryAsync(req, ct)).ToHttp(c => Results.Created($"/api/v1/categories/{c.Id}", c)))
            .Validate<UpsertCategoryRequest>().RequireAuthorization(Policies.Admin);
        admin.MapPut("/categories/{id:guid}", async (Guid id, UpsertCategoryRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.UpdateCategoryAsync(id, req, ct)).ToHttp()).Validate<UpsertCategoryRequest>().RequireAuthorization(Policies.Admin);
        admin.MapDelete("/categories/{id:guid}", async (Guid id, CatalogService svc, CancellationToken ct) =>
            (await svc.DeleteCategoryAsync(id, ct)).ToHttp(_ => Results.NoContent())).RequireAuthorization(Policies.Admin);

        admin.MapPost("/branches", async (CreateBranchRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.CreateBranchAsync(req, ct)).ToHttp(b => Results.Created($"/api/v1/branches/{b.Id}", b)))
            .Validate<CreateBranchRequest>().RequireAuthorization(Policies.Admin);

        admin.MapPost("/products", async (UpsertProductRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.CreateProductAsync(req, ct)).ToHttp(id => Results.Created($"/api/v1/admin/products/{id}", new { id })))
            .Validate<UpsertProductRequest>();
        admin.MapGet("/products/{id:guid}", async (Guid id, CatalogService svc, CancellationToken ct) => (await svc.GetProductAdminAsync(id, ct)).ToHttp());
        admin.MapPut("/products/{id:guid}", async (Guid id, UpsertProductRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.UpdateProductAsync(id, req, ct)).ToHttp(_ => Results.NoContent())).Validate<UpsertProductRequest>();
        admin.MapDelete("/products/{id:guid}", async (Guid id, CatalogService svc, CancellationToken ct) =>
            (await svc.DeleteProductAsync(id, ct)).ToHttp(_ => Results.NoContent()));

        admin.MapGet("/pieces", async (Guid? productId, CatalogService svc, CancellationToken ct) => Results.Ok(await svc.GetPiecesAsync(productId, ct)));
        admin.MapPost("/pieces", async (UpsertPieceRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.CreatePieceAsync(req, ct)).ToHttp(id => Results.Created($"/api/v1/admin/pieces/{id}", new { id })))
            .Validate<UpsertPieceRequest>();
        admin.MapPut("/pieces/{id:guid}", async (Guid id, UpsertPieceRequest req, CatalogService svc, CancellationToken ct) =>
            (await svc.UpdatePieceAsync(id, req, ct)).ToHttp(_ => Results.NoContent())).Validate<UpsertPieceRequest>();
        admin.MapDelete("/pieces/{id:guid}", async (Guid id, CatalogService svc, CancellationToken ct) =>
            (await svc.DeletePieceAsync(id, ct)).ToHttp(_ => Results.NoContent()));

        return api;
    }
}

public sealed record ProductListQueryParams(
    string? Category, Audience? Audience, GoldColor? Color, decimal? MinGrams, decimal? MaxGrams,
    long? MaxPriceRial, string? Search, string? Sort, int? Page, int? PageSize)
{
    public ProductListQuery ToQuery() => new(Category, Audience, Color, MinGrams, MaxGrams, MaxPriceRial, Search,
        Sort ?? "popular", Page ?? 1, PageSize ?? 24);
}
