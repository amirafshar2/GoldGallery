using Gold.Domain.Common;

namespace Gold.Domain.Pricing;

public enum RoundingDirection
{
    /// <summary>Round down — in the customer's favour (default).</summary>
    Down = 1,
    Nearest = 2,
    Up = 3,
}

/// <summary>
/// Final-price rounding, applied ONCE after all components are summed.
/// Example: increment 10_000 rial = round to the nearest 1,000 toman.
/// </summary>
public sealed record RoundingPolicy
{
    public long IncrementRial { get; }
    public RoundingDirection Direction { get; }

    public RoundingPolicy(long incrementRial, RoundingDirection direction)
    {
        if (incrementRial < 1) throw new DomainException("Rounding increment must be at least 1 rial.");
        IncrementRial = incrementRial;
        Direction = direction;
    }

    public static RoundingPolicy None => new(1, RoundingDirection.Nearest);

    public long Apply(long rial)
    {
        if (IncrementRial == 1) return rial;
        var remainder = rial % IncrementRial;
        if (remainder == 0) return rial;
        var down = rial - remainder;
        return Direction switch
        {
            RoundingDirection.Down => down,
            RoundingDirection.Up => down + IncrementRial,
            _ => remainder * 2 >= IncrementRial ? down + IncrementRial : down,
        };
    }
}
