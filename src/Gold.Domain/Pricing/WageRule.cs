using Gold.Domain.Common;
using Gold.Domain.ValueObjects;

namespace Gold.Domain.Pricing;

public enum WageKind
{
    /// <summary>Wage as a percentage of the raw gold value (most common, e.g. 14%).</summary>
    PercentOfRawGold = 1,
    /// <summary>Fixed rial amount per gram of gold.</summary>
    FixedPerGram = 2,
}

/// <summary>Manufacturing wage (اجرت ساخت) rule, configurable per product or category.</summary>
public sealed record WageRule
{
    public WageKind Kind { get; }
    public Percentage Percent { get; }
    public Money PerGram { get; }

    private WageRule(WageKind kind, Percentage percent, Money perGram)
    {
        Kind = kind;
        Percent = percent;
        PerGram = perGram;
    }

    public static WageRule PercentOfRaw(Percentage percent) => new(WageKind.PercentOfRawGold, percent, Money.Zero);

    public static WageRule FixedPerGram(Money perGram)
    {
        if (perGram.Rial <= 0) throw new DomainException("Fixed wage per gram must be positive.");
        return new(WageKind.FixedPerGram, Percentage.Zero, perGram);
    }

    internal decimal Calculate(decimal rawGoldRial, Weight weight) => Kind switch
    {
        WageKind.PercentOfRawGold => Percent.Of(rawGoldRial),
        WageKind.FixedPerGram => PerGram.Rial * weight.Grams,
        _ => throw new DomainException("Unknown wage kind."),
    };
}
