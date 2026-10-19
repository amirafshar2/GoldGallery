using Gold.Domain.ValueObjects;

namespace Gold.Domain.Pricing;

/// <summary>
/// Transparent invoice lines shown to the customer before payment.
/// Each line is rounded to whole rial; Total = sum of lines + RoundingAdjustment (≤ 0 when rounding down).
/// </summary>
public sealed record PriceBreakdown(
    Weight Weight,
    Karat Karat,
    Money RatePerGram18,
    Money RawGold,
    Money Wage,
    Money Profit,
    Money Tax,
    Money StoneValue,
    long RoundingAdjustmentRial,
    Money Total)
{
    public Money Subtotal => RawGold + Wage + Profit + Tax + StoneValue;
}

/// <summary>Result of valuing old gold that a customer sells to the shop. No wage is refunded.</summary>
public sealed record BuyBackValuation(
    Weight Weight,
    Karat Karat,
    Money BuyRatePerGram18,
    Money Value);
