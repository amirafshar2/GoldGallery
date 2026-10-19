using Gold.Domain.Common;

namespace Gold.Domain.ValueObjects;

/// <summary>
/// Amount in Iranian Rial stored as long. Display in Toman (Rial / 10).
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public long Rial { get; }

    private Money(long rial) => Rial = rial;

    public static Money Zero => new(0);

    public static Money FromRial(long rial)
    {
        if (rial < 0) throw new DomainException("Money cannot be negative.");
        return new Money(rial);
    }

    public static Money FromToman(long toman) => FromRial(checked(toman * 10));

    /// <summary>Rounds a decimal rial value to whole rial (half away from zero).</summary>
    public static Money FromDecimalRial(decimal rial) =>
        FromRial((long)decimal.Round(rial, 0, MidpointRounding.AwayFromZero));

    public decimal Toman => Rial / 10m;

    public static Money operator +(Money a, Money b) => new(checked(a.Rial + b.Rial));
    public static Money operator -(Money a, Money b) => FromRial(a.Rial - b.Rial);

    public int CompareTo(Money other) => Rial.CompareTo(other.Rial);

    public override string ToString() => $"{Rial:N0} IRR";
}
