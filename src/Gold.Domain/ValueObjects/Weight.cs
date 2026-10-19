using Gold.Domain.Common;

namespace Gold.Domain.ValueObjects;

/// <summary>
/// Gold weight stored as whole milligrams (never double/float).
/// 1 gram = 1000 mg. Iranian scales show grams with three decimals, which maps exactly to mg.
/// </summary>
public readonly record struct Weight
{
    /// <summary>1 mesghal = 4.608 g (used when buying old gold).</summary>
    public const long MilligramsPerMesghal = 4_608;

    public long Milligrams { get; }

    private Weight(long milligrams) => Milligrams = milligrams;

    public static Weight Zero => new(0);

    public static Weight FromMilligrams(long milligrams)
    {
        if (milligrams < 0) throw new DomainException("Weight cannot be negative.");
        return new Weight(milligrams);
    }

    /// <summary>Accepts grams with at most three decimals (e.g. 1.440).</summary>
    public static Weight FromGrams(decimal grams)
    {
        if (grams < 0) throw new DomainException("Weight cannot be negative.");
        var mg = grams * 1000m;
        if (mg != decimal.Truncate(mg))
            throw new DomainException("Weight supports at most three decimal places (milligram precision).");
        return new Weight((long)mg);
    }

    public static Weight FromMesghal(decimal mesghal) =>
        FromMilligrams((long)decimal.Round(mesghal * MilligramsPerMesghal, 0, MidpointRounding.AwayFromZero));

    public decimal Grams => Milligrams / 1000m;
    public decimal Mesghal => Milligrams / (decimal)MilligramsPerMesghal;

    public bool IsZero => Milligrams == 0;

    public static Weight operator +(Weight a, Weight b) => new(a.Milligrams + b.Milligrams);
    public static Weight operator -(Weight a, Weight b) => FromMilligrams(a.Milligrams - b.Milligrams);

    public override string ToString() => $"{Grams:0.000} g";
}
