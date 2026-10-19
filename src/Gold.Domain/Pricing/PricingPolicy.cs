using Gold.Domain.ValueObjects;

namespace Gold.Domain.Pricing;

/// <summary>
/// Shop-level pricing settings. All values come from the admin panel with a validity date —
/// never hard-coded. Confirm tax and profit rules with the accountant/guild before go-live.
/// </summary>
public sealed record PricingPolicy(
    Percentage Profit,
    Percentage Tax,
    RoundingPolicy Rounding)
{
    /// <summary>Demo defaults matching common Iranian practice (7% profit, 10% tax on wage+profit).</summary>
    public static PricingPolicy Demo => new(
        Percentage.FromPercent(7),
        Percentage.FromPercent(10),
        new RoundingPolicy(10_000, RoundingDirection.Down));
}
