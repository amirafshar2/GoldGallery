using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Application.Rates;
using Gold.Domain.Common;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Catalog;

public sealed class CatalogService(IAppDbContext db, RateService rates, IClock clock, ICurrentUser user, RateGuard guard)
{
    // ---------- public reads ----------

    public Task<List<CategoryDto>> GetCategoriesAsync(CancellationToken ct) =>
        db.Categories.AsNoTracking().OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Slug, c.Name, c.SortOrder)).ToListAsync(ct);

    public Task<List<BranchDto>> GetBranchesAsync(CancellationToken ct) =>
        db.Branches.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new BranchDto(b.Id, b.Name, b.Address)).ToListAsync(ct);

    public async Task<PagedResult<ProductCardDto>> GetProductsAsync(ProductListQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 60);

        var query = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q.Category)) query = query.Where(p => p.Category!.Slug == q.Category);
        if (q.Audience is not null) query = query.Where(p => p.Audience == q.Audience);
        if (q.Color is not null) query = query.Where(p => p.Color == q.Color);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(p => p.Name.Contains(q.Search));
        if (q.MinGrams is not null)
        {
            var mg = (long)(q.MinGrams.Value * 1000);
            query = query.Where(p => p.Pieces.Any(x => x.Status == PieceStatus.Available && !x.IsDeleted && x.GoldWeightMg >= mg));
        }
        if (q.MaxGrams is not null)
        {
            var mg = (long)(q.MaxGrams.Value * 1000);
            query = query.Where(p => p.Pieces.Any(x => x.Status == PieceStatus.Available && !x.IsDeleted && x.GoldWeightMg <= mg));
        }

        // Projection: only what the card needs (no entity leaves the service).
        var rows = await query.Select(p => new
        {
            p.Id, p.Slug, p.Name, CategoryName = p.Category!.Name, p.Audience, p.Color, p.ImageUrl,
            p.WageKind, p.WageValue, p.IsFeatured, p.Popularity, p.CreatedAt,
            Pieces = p.Pieces.Where(x => x.Status == PieceStatus.Available && !x.IsDeleted)
                .Select(x => new { x.GoldWeightMg, x.KaratPerMille, x.StoneValueRial }).ToList(),
        }).ToListAsync(ct);

        var (rate, _) = await rates.GetLatestTwoAsync(ct);
        var policy = await rates.GetPolicyAsync(ct);

        var cards = rows.Select(r =>
        {
            long? from = null;
            if (rate is not null && r.Pieces.Count > 0)
            {
                var wage = ToWage(r.WageKind, r.WageValue);
                from = r.Pieces.Min(x => PricingEngine.CalculateSale(
                    Weight.FromMilligrams(x.GoldWeightMg), Karat.FromPerMille(x.KaratPerMille), rate.SellPerGram18,
                    wage, policy, Money.FromRial(x.StoneValueRial)).Total.Rial);
            }
            return new ProductCardDto(r.Id, r.Slug, r.Name, r.CategoryName, r.Audience, r.Color, r.ImageUrl,
                r.WageKind == WageKind.PercentOfRawGold ? (int)r.WageValue : 0,
                from, r.Pieces.Count > 0 ? r.Pieces.Min(x => x.GoldWeightMg) / 1000m : null,
                r.Pieces.Count, r.IsFeatured);
        });

        if (q.MaxPriceRial is not null) cards = cards.Where(c => c.StartingFromRial <= q.MaxPriceRial);

        cards = q.Sort switch
        {
            "cheapest" => cards.OrderBy(c => c.StartingFromRial ?? long.MaxValue),
            "expensive" => cards.OrderByDescending(c => c.StartingFromRial ?? 0),
            "low-wage" => cards.OrderBy(c => c.WageBp),
            "lightest" => cards.OrderBy(c => c.LightestGrams ?? decimal.MaxValue),
            "newest" => cards.OrderByDescending(c => rows.First(r => r.Id == c.Id).CreatedAt),
            _ => cards.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => rows.First(r => r.Id == c.Id).Popularity),
        };

        var list = cards.ToList();
        return new PagedResult<ProductCardDto>(list.Skip((page - 1) * size).Take(size).ToList(), page, size, list.Count);
    }

    public async Task<Result<ProductDetailDto>> GetProductAsync(string slug, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new
            {
                x.Id, x.Slug, x.Name, x.Description, CategoryName = x.Category!.Name, x.Audience, x.Color, x.ImageUrl,
                x.WageKind, x.WageValue,
                Pieces = x.Pieces.Where(z => !z.IsDeleted && z.Status != PieceStatus.Sold)
                    .Select(z => new { z.Id, Branch = z.Branch!.Name, z.GoldWeightMg, z.KaratPerMille, z.Size, z.Status, z.StoneValueRial })
                    .ToList(),
            }).FirstOrDefaultAsync(ct);
        if (p is null) return Error.NotFound("product.notFound", "محصول پیدا نشد.");

        var (rate, prev) = await rates.GetLatestTwoAsync(ct);
        var policy = await rates.GetPolicyAsync(ct);
        var wage = ToWage(p.WageKind, p.WageValue);

        var pieces = p.Pieces.OrderBy(z => z.GoldWeightMg).Select(z =>
        {
            if (rate is null)
                return new PiecePriceDto(z.Id, z.Branch, z.GoldWeightMg / 1000m, z.KaratPerMille, z.Size, z.Status.ToString(), 0, 0, 0, 0, z.StoneValueRial, 0);
            var b = PricingEngine.CalculateSale(Weight.FromMilligrams(z.GoldWeightMg), Karat.FromPerMille(z.KaratPerMille),
                rate.SellPerGram18, wage, policy, Money.FromRial(z.StoneValueRial));
            return new PiecePriceDto(z.Id, z.Branch, z.GoldWeightMg / 1000m, z.KaratPerMille, z.Size, z.Status.ToString(),
                b.RawGold.Rial, b.Wage.Rial, b.Profit.Rial, b.Tax.Rial, b.StoneValue.Rial, b.Total.Rial);
        }).ToList();

        var canSell = rate is not null && guard.CanSellOnline(rate, prev, clock.Now);
        return new ProductDetailDto(p.Id, p.Slug, p.Name, p.Description, p.CategoryName, p.Audience, p.Color, p.ImageUrl,
            p.WageKind, p.WageValue, rate?.SellPerGram18.Rial ?? 0, canSell, pieces);
    }

    // ---------- admin CRUD ----------

    public async Task<Result<CategoryDto>> CreateCategoryAsync(UpsertCategoryRequest r, CancellationToken ct)
    {
        if (await db.Categories.AnyAsync(c => c.Slug == r.Slug, ct)) return Error.Conflict("category.slug", "این نامک قبلاً استفاده شده است.");
        var c = Category.Create(r.Slug, r.Name, r.SortOrder);
        db.Categories.Add(c);
        Audit("category.create", "Category", c.Id, r.Name);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(c.Id, c.Slug, c.Name, c.SortOrder);
    }

    public async Task<Result<CategoryDto>> UpdateCategoryAsync(Guid id, UpsertCategoryRequest r, CancellationToken ct)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return Error.NotFound("category.notFound", "دسته پیدا نشد.");
        c.Update(r.Name, r.SortOrder);
        Audit("category.update", "Category", c.Id, r.Name);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(c.Id, c.Slug, c.Name, c.SortOrder);
    }

    public async Task<Result<Unit>> DeleteCategoryAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return Error.NotFound("category.notFound", "دسته پیدا نشد.");
        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            return Error.Conflict("category.inUse", "این دسته محصول دارد و حذف نمی‌شود.");
        c.SoftDelete();
        Audit("category.delete", "Category", c.Id, c.Name);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<BranchDto>> CreateBranchAsync(CreateBranchRequest r, CancellationToken ct)
    {
        var b = Branch.Create(r.Name, r.Address);
        db.Branches.Add(b);
        Audit("branch.create", "Branch", b.Id, r.Name);
        await db.SaveChangesAsync(ct);
        return new BranchDto(b.Id, b.Name, b.Address);
    }

    public async Task<Result<Guid>> CreateProductAsync(UpsertProductRequest r, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId, ct)) return Error.Validation("product.category", "دسته معتبر نیست.");
        if (await db.Products.IgnoreQueryFilters().AnyAsync(p => p.Slug == r.Slug.ToLower(), ct))
            return Error.Conflict("product.slug", "این نامک قبلاً استفاده شده است.");
        try
        {
            var p = Product.Create(r.Slug, r.Name, r.CategoryId, r.Audience, r.Color, ToWage(r.WageKind, r.WageValue),
                r.Description, r.ImageUrl, r.IsFeatured);
            db.Products.Add(p);
            Audit("product.create", "Product", p.Id, r.Name);
            await db.SaveChangesAsync(ct);
            return p.Id;
        }
        catch (DomainException ex) { return Error.Validation("product.invalid", ex.Message); }
    }

    public async Task<Result<Unit>> UpdateProductAsync(Guid id, UpsertProductRequest r, CancellationToken ct)
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Error.NotFound("product.notFound", "محصول پیدا نشد.");
        if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId, ct)) return Error.Validation("product.category", "دسته معتبر نیست.");
        try
        {
            p.Update(r.Name, r.Description, r.Audience, r.Color, ToWage(r.WageKind, r.WageValue), r.ImageUrl, r.IsFeatured);
            p.MoveToCategory(r.CategoryId);
        }
        catch (DomainException ex) { return Error.Validation("product.invalid", ex.Message); }
        Audit("product.update", "Product", p.Id, $"wage={r.WageKind}:{r.WageValue}");
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<Unit>> DeleteProductAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return Error.NotFound("product.notFound", "محصول پیدا نشد.");
        if (await db.Pieces.AnyAsync(x => x.ProductId == id && x.Status != PieceStatus.Available, ct))
            return Error.Conflict("product.hasActivePieces", "این محصول قطعه‌ی رزرو یا فروخته‌شده دارد.");
        p.SoftDelete();
        foreach (var piece in await db.Pieces.Where(x => x.ProductId == id).ToListAsync(ct)) piece.SoftDelete();
        Audit("product.delete", "Product", p.Id, p.Name);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<ProductAdminDto>> GetProductAdminAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new ProductAdminDto(x.Id, x.Slug, x.Name, x.Description, x.CategoryId, x.Audience, x.Color, x.WageKind, x.WageValue, x.ImageUrl, x.IsFeatured))
            .FirstOrDefaultAsync(ct);
        return p is null ? Error.NotFound("product.notFound", "محصول پیدا نشد.") : p;
    }

    public Task<List<PieceAdminDto>> GetPiecesAsync(Guid? productId, CancellationToken ct) =>
        db.Pieces.AsNoTracking()
            .Where(x => productId == null || x.ProductId == productId)
            .OrderBy(x => x.Product!.Name).ThenBy(x => x.GoldWeightMg)
            .Select(x => new PieceAdminDto(x.Id, x.ProductId, x.Product!.Name, x.BranchId, x.Branch!.Name, x.Barcode,
                x.GoldWeightMg / 1000m, x.KaratPerMille, x.Size, x.StoneWeightMg / 1000m, x.StoneValueRial, x.Status.ToString()))
            .ToListAsync(ct);

    public async Task<Result<Guid>> CreatePieceAsync(UpsertPieceRequest r, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == r.ProductId, ct)) return Error.Validation("piece.product", "محصول معتبر نیست.");
        if (!await db.Branches.AnyAsync(b => b.Id == r.BranchId, ct)) return Error.Validation("piece.branch", "شعبه معتبر نیست.");
        if (await db.Pieces.IgnoreQueryFilters().AnyAsync(p => p.Barcode == r.Barcode, ct)) return Error.Conflict("piece.barcode", "این بارکد تکراری است.");
        try
        {
            var piece = Piece.Create(r.ProductId, r.BranchId, r.Barcode, Weight.FromGrams(r.GoldGrams), Karat.FromPerMille(r.Karat),
                r.Size, Weight.FromGrams(r.StoneGrams), Money.FromRial(r.StoneValueRial));
            db.Pieces.Add(piece);
            Audit("piece.create", "Piece", piece.Id, $"barcode={r.Barcode};g={r.GoldGrams};k={r.Karat}");
            await db.SaveChangesAsync(ct);
            return piece.Id;
        }
        catch (DomainException ex) { return Error.Validation("piece.invalid", ex.Message); }
    }

    public async Task<Result<Unit>> UpdatePieceAsync(Guid id, UpsertPieceRequest r, CancellationToken ct)
    {
        var piece = await db.Pieces.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (piece is null) return Error.NotFound("piece.notFound", "قطعه پیدا نشد.");
        var oldWeight = piece.GoldWeightMg;
        try
        {
            piece.Update(Weight.FromGrams(r.GoldGrams), Karat.FromPerMille(r.Karat), r.Size,
                Weight.FromGrams(r.StoneGrams), Money.FromRial(r.StoneValueRial), r.BranchId);
        }
        catch (DomainException ex) { return Error.Validation("piece.invalid", ex.Message); }
        Audit("piece.update", "Piece", piece.Id, $"weightMg {oldWeight}->{piece.GoldWeightMg}");
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<Unit>> DeletePieceAsync(Guid id, CancellationToken ct)
    {
        var piece = await db.Pieces.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (piece is null) return Error.NotFound("piece.notFound", "قطعه پیدا نشد.");
        try { piece.SoftDelete(); }
        catch (DomainException ex) { return Error.Conflict("piece.notAvailable", ex.Message); }
        Audit("piece.delete", "Piece", piece.Id, piece.Barcode);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    internal static WageRule ToWage(WageKind kind, long value) => kind == WageKind.PercentOfRawGold
        ? WageRule.PercentOfRaw(Percentage.FromBasisPoints((int)value))
        : WageRule.FixedPerGram(Money.FromRial(value));

    private void Audit(string action, string entity, Guid id, string details) =>
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, action, entity, id.ToString(), details, user.IsDemo));
}
