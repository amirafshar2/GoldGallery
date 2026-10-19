using Gold.Domain.Common;

namespace Gold.Domain.ValueObjects;

/// <summary>
/// Percentage stored as basis points (1 bp = 0.01%). 7% = 700, 10% = 1000, 14% = 1400.
/// Avoids floating point in every money calculation.
/// </summary>
public readonly record struct Percentage
{
    public int BasisPoints { get; }

    private Percentage(int bp) => BasisPoints = bp;

    public static Percentage Zero => new(0);

    public static Percentage FromBasisPoints(int bp)
    {
        if (bp is < 0 or > 100_000)
            throw new DomainException("Percentage must be between 0 and 1000%.");
        return new Percentage(bp);
    }

    public static Percentage FromPercent(decimal percent)
    {
        var bp = percent * 100m;
        if (bp != decimal.Truncate(bp))
            throw new DomainException("Percentage supports at most two decimal places.");
        return FromBasisPoints((int)bp);
    }

    public decimal Of(decimal value) => value * BasisPoints / 10_000m;

    public override string ToString() => $"{BasisPoints / 100m:0.##}%";
}
