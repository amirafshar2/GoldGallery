using Gold.Domain.Common;

namespace Gold.Domain.ValueObjects;

/// <summary>
/// Gold purity in parts per thousand (عیار). 750 = 18K, 900 = coin (21.6K), 995/999 = bullion.
/// Old gold may have any purity, so values between 1 and 1000 are allowed.
/// </summary>
public readonly record struct Karat
{
    /// <summary>The reference purity for Iranian daily rates (price per gram of 18K).</summary>
    public const int ReferencePerMille = 750;

    public int PerMille { get; }

    private Karat(int perMille) => PerMille = perMille;

    public static Karat K18 => new(750);
    public static Karat Coin => new(900);
    public static Karat Bullion995 => new(995);
    public static Karat Bullion999 => new(999);

    public static Karat FromPerMille(int perMille)
    {
        if (perMille is < 1 or > 1000)
            throw new DomainException("Karat must be between 1 and 1000 parts per thousand.");
        return new Karat(perMille);
    }

    /// <summary>Factor relative to 18K reference, as an exact decimal.</summary>
    public decimal FactorToReference => PerMille / (decimal)ReferencePerMille;

    public override string ToString() => PerMille.ToString();
}
