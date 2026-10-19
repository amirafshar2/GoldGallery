using Gold.Domain.Entities;
using Gold.Domain.Pricing;

namespace Gold.Application.Catalog;

public sealed record CategoryDto(Guid Id, string Slug, string Name, int SortOrder);
public sealed record UpsertCategoryRequest(string Slug, string Name, int SortOrder);

public sealed record BranchDto(Guid Id, string Name, string Address);
public sealed record CreateBranchRequest(string Name, string Address);

public sealed record ProductListQuery(
    string? Category = null, Audience? Audience = null, GoldColor? Color = null,
    decimal? MinGrams = null, decimal? MaxGrams = null, long? MaxPriceRial = null,
    string? Search = null, string Sort = "popular", int Page = 1, int PageSize = 24);

public sealed record ProductCardDto(
    Guid Id, string Slug, string Name, string CategoryName, Audience Audience, GoldColor Color,
    string ImageUrl, int WageBp, long? StartingFromRial, decimal? LightestGrams, int AvailablePieces, bool IsFeatured);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record PiecePriceDto(
    Guid PieceId, string Branch, decimal GoldGrams, int Karat, string Size, string Status,
    long RawGoldRial, long WageRial, long ProfitRial, long TaxRial, long StoneValueRial, long TotalRial);

public sealed record ProductDetailDto(
    Guid Id, string Slug, string Name, string Description, string CategoryName, Audience Audience, GoldColor Color,
    string ImageUrl, WageKind WageKind, long WageValue, long RateSellPerGram18Rial, bool CanSellOnline,
    IReadOnlyList<PiecePriceDto> Pieces);

public sealed record UpsertProductRequest(
    string Slug, string Name, string Description, Guid CategoryId, Audience Audience, GoldColor Color,
    WageKind WageKind, long WageValue, string ImageUrl, bool IsFeatured);

public sealed record UpsertPieceRequest(
    Guid ProductId, Guid BranchId, string Barcode, decimal GoldGrams, int Karat, string Size,
    decimal StoneGrams, long StoneValueRial);

public sealed record PieceAdminDto(
    Guid Id, Guid ProductId, string ProductName, Guid BranchId, string Branch, string Barcode,
    decimal GoldGrams, int Karat, string Size, decimal StoneGrams, long StoneValueRial, string Status);

public sealed record ProductAdminDto(
    Guid Id, string Slug, string Name, string Description, Guid CategoryId, Audience Audience, GoldColor Color,
    WageKind WageKind, long WageValue, string ImageUrl, bool IsFeatured);
