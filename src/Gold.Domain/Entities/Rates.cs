using Gold.Domain.Common;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;

namespace Gold.Domain.Entities;

/// <summary>Append-only history of 18K rates. Never updated or deleted.</summary>
public sealed class GoldRateEntry
{
    public long Id { get; private set; }
    public long SellPerGram18Rial { get; private set; }
    public long BuyPerGram18Rial { get; private set; }
    public DateTimeOffset AsOf { get; private set; }
    public string Source { get; private set; } = default!;
    public Guid? EnteredByUserId { get; private set; }

    private GoldRateEntry() { }

    public static GoldRateEntry From(GoldRate rate, Guid? userId) => new()
    {
        SellPerGram18Rial = rate.SellPerGram18.Rial,
        BuyPerGram18Rial = rate.BuyPerGram18.Rial,
        AsOf = rate.AsOf,
        Source = rate.Source,
        EnteredByUserId = userId,
    };

    public GoldRate ToRate() => GoldRate.Create(
        Money.FromRial(SellPerGram18Rial), Money.FromRial(BuyPerGram18Rial), AsOf, Source);
}

/// <summary>Versioned pricing settings (profit, tax, rounding). The newest ValidFrom ≤ now wins.</summary>
public sealed class PricingSettings
{
    public long Id { get; private set; }
    public int ProfitBp { get; private set; }
    public int TaxBp { get; private set; }
    public long RoundingIncrementRial { get; private set; }
    public RoundingDirection RoundingDirection { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    private PricingSettings() { }

    public static PricingSettings Create(PricingPolicy policy, DateTimeOffset validFrom, Guid? userId) => new()
    {
        ProfitBp = policy.Profit.BasisPoints,
        TaxBp = policy.Tax.BasisPoints,
        RoundingIncrementRial = policy.Rounding.IncrementRial,
        RoundingDirection = policy.Rounding.Direction,
        ValidFrom = validFrom,
        ChangedByUserId = userId,
    };

    public PricingPolicy ToPolicy() => new(
        Percentage.FromBasisPoints(ProfitBp),
        Percentage.FromBasisPoints(TaxBp),
        new RoundingPolicy(RoundingIncrementRial, RoundingDirection));
}
