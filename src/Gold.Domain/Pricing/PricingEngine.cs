using Gold.Domain.Common;
using Gold.Domain.ValueObjects;

namespace Gold.Domain.Pricing;

/// <summary>
/// Pure, dependency-free gold pricing. The heart of the system — keep 100% test coverage.
///
///   raw    = weight(g) × rate18(per g) × karat/750
///   wage   = rule (percent of raw | fixed × grams)
///   profit = profit% × (raw + wage)
///   tax    = tax% × (wage + profit)            ← never on raw gold
///   total  = raw + wage + profit + tax + stone, then rounded once by policy
/// </summary>
public static class PricingEngine
{
    public static PriceBreakdown CalculateSale(
        Weight goldWeight,
        Karat karat,
        Money ratePerGram18,
        WageRule wage,
        PricingPolicy policy,
        Money? stoneValue = null)
    {
        ArgumentNullException.ThrowIfNull(wage);
        ArgumentNullException.ThrowIfNull(policy);
        if (goldWeight.IsZero) throw new DomainException("Gold weight must be greater than zero.");
        if (ratePerGram18.Rial <= 0) throw new DomainException("Gold rate must be greater than zero.");

        var stone = stoneValue ?? Money.Zero;

        var rawExact = goldWeight.Grams * ratePerGram18.Rial * karat.FactorToReference;
        var raw = Money.FromDecimalRial(rawExact);

        var wageMoney = Money.FromDecimalRial(wage.Calculate(raw.Rial, goldWeight));
        var profit = Money.FromDecimalRial(policy.Profit.Of(raw.Rial + wageMoney.Rial));
        var tax = Money.FromDecimalRial(policy.Tax.Of(wageMoney.Rial + profit.Rial));

        var subtotal = raw + wageMoney + profit + tax + stone;
        var rounded = policy.Rounding.Apply(subtotal.Rial);

        return new PriceBreakdown(
            goldWeight, karat, ratePerGram18,
            raw, wageMoney, profit, tax, stone,
            rounded - subtotal.Rial,
            Money.FromRial(rounded));
    }

    /// <summary>Old gold purchase: weight × buy-rate × karat/750, rounded down to whole rial. No wage.</summary>
    public static BuyBackValuation CalculateBuyBack(Weight weight, Karat karat, Money buyRatePerGram18)
    {
        if (weight.IsZero) throw new DomainException("Gold weight must be greater than zero.");
        if (buyRatePerGram18.Rial <= 0) throw new DomainException("Buy rate must be greater than zero.");

        var exact = weight.Grams * buyRatePerGram18.Rial * karat.FactorToReference;
        var value = Money.FromRial((long)decimal.Floor(exact));
        return new BuyBackValuation(weight, karat, buyRatePerGram18, value);
    }
}
