using Gold.Domain.Common;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;

namespace Gold.Domain.Entities;

public sealed class Category : Entity
{
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int SortOrder { get; private set; }

    private Category() { }

    public static Category Create(string slug, string name, int sortOrder = 0) =>
        new() { Slug = slug, Name = name, SortOrder = sortOrder };

    public void Update(string name, int sortOrder) { Name = name; SortOrder = sortOrder; }
}

public sealed class Branch : Entity
{
    public string Name { get; private set; } = default!;
    public string Address { get; private set; } = "";

    private Branch() { }

    public static Branch Create(string name, string address = "") => new() { Name = name, Address = address };
}

/// <summary>A design/model (مدل). Real stock lives in <see cref="Piece"/> — every piece has its own weight.</summary>
public sealed class Product : Entity
{
    public string Slug { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = "";
    public Guid CategoryId { get; private set; }
    public Category? Category { get; private set; }
    public Audience Audience { get; private set; }
    public GoldColor Color { get; private set; }
    public WageKind WageKind { get; private set; }
    /// <summary>Basis points when <see cref="WageKind.PercentOfRawGold"/>, rial per gram when FixedPerGram.</summary>
    public long WageValue { get; private set; }
    public string ImageUrl { get; private set; } = "";
    public bool IsFeatured { get; private set; }
    public int Popularity { get; private set; }

    private readonly List<Piece> _pieces = [];
    public IReadOnlyCollection<Piece> Pieces => _pieces;

    private Product() { }

    public static Product Create(string slug, string name, Guid categoryId, Audience audience, GoldColor color,
        WageRule wage, string description = "", string imageUrl = "", bool isFeatured = false)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new DomainException("Slug is required.");
        var p = new Product { Slug = slug.Trim().ToLowerInvariant(), CategoryId = categoryId };
        p.Update(name, description, audience, color, wage, imageUrl, isFeatured);
        return p;
    }

    public void Update(string name, string description, Audience audience, GoldColor color, WageRule wage,
        string imageUrl, bool isFeatured)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        Name = name.Trim();
        Description = description;
        Audience = audience;
        Color = color;
        WageKind = wage.Kind;
        WageValue = wage.Kind == WageKind.PercentOfRawGold ? wage.Percent.BasisPoints : wage.PerGram.Rial;
        ImageUrl = imageUrl;
        IsFeatured = isFeatured;
    }

    public void MoveToCategory(Guid categoryId) => CategoryId = categoryId;

    public WageRule Wage => WageKind == WageKind.PercentOfRawGold
        ? WageRule.PercentOfRaw(Percentage.FromBasisPoints((int)WageValue))
        : WageRule.FixedPerGram(Money.FromRial(WageValue));
}

/// <summary>A single physical item with its own weight, size and branch (قطعه‌ی یکتا).</summary>
public sealed class Piece : Entity, IConcurrencyStamped
{
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Guid BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public string Barcode { get; private set; } = default!;
    public long GoldWeightMg { get; private set; }
    public long StoneWeightMg { get; private set; }
    public int KaratPerMille { get; private set; }
    public string Size { get; private set; } = "";
    public long StoneValueRial { get; private set; }
    public PieceStatus Status { get; private set; } = PieceStatus.Available;
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    private Piece() { }

    public static Piece Create(Guid productId, Guid branchId, string barcode, Weight goldWeight, Karat karat,
        string size = "", Weight? stoneWeight = null, Money? stoneValue = null)
    {
        var p = new Piece { ProductId = productId, BranchId = branchId, Barcode = barcode };
        p.Update(goldWeight, karat, size, stoneWeight ?? Weight.Zero, stoneValue ?? Money.Zero, branchId);
        return p;
    }

    public void Update(Weight goldWeight, Karat karat, string size, Weight stoneWeight, Money stoneValue, Guid branchId)
    {
        if (Status != PieceStatus.Available) throw new DomainException("Only available pieces can be edited.");
        if (goldWeight.IsZero) throw new DomainException("Gold weight must be greater than zero.");
        GoldWeightMg = goldWeight.Milligrams;
        KaratPerMille = karat.PerMille;
        Size = size;
        StoneWeightMg = stoneWeight.Milligrams;
        StoneValueRial = stoneValue.Rial;
        BranchId = branchId;
        Touch();
    }

    public Weight GoldWeight => Weight.FromMilligrams(GoldWeightMg);
    public Karat Karat => Karat.FromPerMille(KaratPerMille);
    public Money StoneValue => Money.FromRial(StoneValueRial);

    public void Reserve()
    {
        if (Status != PieceStatus.Available)
            throw new DomainException("This piece was just reserved by another customer.");
        Status = PieceStatus.Reserved;
        Touch();
    }

    public void Release()
    {
        if (Status == PieceStatus.Reserved) { Status = PieceStatus.Available; Touch(); }
    }

    public void MarkSold()
    {
        if (Status != PieceStatus.Reserved) throw new DomainException("Piece must be reserved before it is sold.");
        Status = PieceStatus.Sold;
        Touch();
    }

    public new void SoftDelete()
    {
        if (Status != PieceStatus.Available) throw new DomainException("Reserved or sold pieces cannot be deleted.");
        base.SoftDelete();
        Touch();
    }

    private void Touch() => ConcurrencyStamp = Guid.NewGuid();
}
