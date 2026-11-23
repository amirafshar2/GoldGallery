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
    /// <summary>Demo defaults: 7% profit, 19% VAT on wage+profit, final price rounded down to whole euros.</summary>
    public static PricingPolicy Demo => new(
        Percentage.FromPercent(7),
        Percentage.FromPercent(19),
        new RoundingPolicy(100, RoundingDirection.Down));
}
