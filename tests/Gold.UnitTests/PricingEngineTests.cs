using Gold.Domain.Common;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;

namespace Gold.UnitTests;

public class PricingEngineTests
{
    private static readonly Money Rate = Money.FromToman(25_895_000); // 258,950,000 rial per gram 18K

    private static PricingPolicy NoRounding => new(
        Percentage.FromPercent(7), Percentage.FromPercent(10), RoundingPolicy.None);

    [Fact]
    public void Sale_RealWorldExample_ProducesTransparentBreakdown()
    {
        // Arrange — ring 1.440 g, 18K, wage 14%, profit 7%, tax 10% on (wage + profit)
        var wage = WageRule.PercentOfRaw(Percentage.FromPercent(14));

        // Act
        var p = PricingEngine.CalculateSale(Weight.FromGrams(1.44m), Karat.K18, Rate, wage, NoRounding);

        // Assert
        Assert.Equal(372_888_000, p.RawGold.Rial);
        Assert.Equal(52_204_320, p.Wage.Rial);
        Assert.Equal(29_756_462, p.Profit.Rial);
        Assert.Equal(8_196_078, p.Tax.Rial);
        Assert.Equal(463_044_860, p.Total.Rial);
        Assert.Equal(0, p.RoundingAdjustmentRial);
    }

    [Fact]
    public void Sale_TaxIsNeverAppliedOnRawGold()
    {
        var policy = new PricingPolicy(Percentage.Zero, Percentage.FromPercent(10), RoundingPolicy.None);
        var wage = WageRule.PercentOfRaw(Percentage.Zero);

        var p = PricingEngine.CalculateSale(Weight.FromGrams(10m), Karat.K18, Rate, wage, policy);

        Assert.Equal(0, p.Tax.Rial);
        Assert.Equal(p.RawGold, p.Total);
    }

    [Fact]
    public void Sale_RoundsOnceAtTheEnd_InCustomersFavour()
    {
        var policy = PricingPolicy.Demo; // round down to 1,000 toman
        var wage = WageRule.PercentOfRaw(Percentage.FromPercent(14));

        var p = PricingEngine.CalculateSale(Weight.FromGrams(1.44m), Karat.K18, Rate, wage, policy);

        Assert.Equal(463_040_000, p.Total.Rial);
        Assert.Equal(-4_860, p.RoundingAdjustmentRial);
        Assert.Equal(p.Subtotal.Rial + p.RoundingAdjustmentRial, p.Total.Rial);
    }

    [Fact]
    public void Sale_FixedWagePerGram()
    {
        var wage = WageRule.FixedPerGram(Money.FromToman(500_000));

        var p = PricingEngine.CalculateSale(Weight.FromGrams(1.44m), Karat.K18, Rate, wage, NoRounding);

        Assert.Equal(7_200_000, p.Wage.Rial);
    }

    [Theory]
    [InlineData(750, 100_000_000)]
    [InlineData(900, 120_000_000)]
    [InlineData(999, 133_200_000)]
    [InlineData(375, 50_000_000)]
    public void Sale_KaratScalesRawGoldRelativeTo18K(int perMille, long expectedRaw)
    {
        var policy = new PricingPolicy(Percentage.Zero, Percentage.Zero, RoundingPolicy.None);

        var p = PricingEngine.CalculateSale(
            Weight.FromGrams(1m), Karat.FromPerMille(perMille), Money.FromRial(100_000_000),
            WageRule.PercentOfRaw(Percentage.Zero), policy);

        Assert.Equal(expectedRaw, p.RawGold.Rial);
    }

    [Fact]
    public void Sale_StoneValueIsAddedSeparately_AndNotTaxed()
    {
        var stone = Money.FromToman(10_000_000);
        var wage = WageRule.PercentOfRaw(Percentage.FromPercent(14));

        var without = PricingEngine.CalculateSale(Weight.FromGrams(2m), Karat.K18, Rate, wage, NoRounding);
        var with = PricingEngine.CalculateSale(Weight.FromGrams(2m), Karat.K18, Rate, wage, NoRounding, stone);

        Assert.Equal(without.Total.Rial + stone.Rial, with.Total.Rial);
        Assert.Equal(without.Tax, with.Tax);
    }

    [Fact]
    public void Sale_ZeroWeight_Throws() =>
        Assert.Throws<DomainException>(() => PricingEngine.CalculateSale(
            Weight.Zero, Karat.K18, Rate, WageRule.PercentOfRaw(Percentage.Zero), NoRounding));

    [Fact]
    public void BuyBack_NoWage_OneMesghalOf740()
    {
        var v = PricingEngine.CalculateBuyBack(
            Weight.FromMesghal(1m), Karat.FromPerMille(740), Money.FromRial(250_000_000));

        Assert.Equal(4_608, v.Weight.Milligrams);
        Assert.Equal(1_136_640_000, v.Value.Rial);
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Weight_RejectsMoreThanThreeDecimals() =>
        Assert.Throws<DomainException>(() => Weight.FromGrams(1.4405m));

    [Fact]
    public void Weight_RejectsNegative() =>
        Assert.Throws<DomainException>(() => Weight.FromGrams(-1m));

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Karat_RejectsOutOfRange(int perMille) =>
        Assert.Throws<DomainException>(() => Karat.FromPerMille(perMille));

    [Fact]
    public void Percentage_UsesBasisPoints()
    {
        Assert.Equal(700, Percentage.FromPercent(7).BasisPoints);
        Assert.Equal(1250, Percentage.FromPercent(12.5m).BasisPoints);
    }

    [Theory]
    [InlineData(463_044_860, 10_000, RoundingDirection.Down, 463_040_000)]
    [InlineData(463_044_860, 10_000, RoundingDirection.Up, 463_050_000)]
    [InlineData(463_045_000, 10_000, RoundingDirection.Nearest, 463_050_000)]
    [InlineData(463_044_999, 10_000, RoundingDirection.Nearest, 463_040_000)]
    [InlineData(463_040_000, 10_000, RoundingDirection.Up, 463_040_000)]
    public void Rounding_Policy(long input, long inc, RoundingDirection dir, long expected) =>
        Assert.Equal(expected, new RoundingPolicy(inc, dir).Apply(input));
}

public class RateGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static GoldRate Rate(long toman, int minutesAgo) =>
        GoldRate.Create(Money.FromToman(toman), Money.FromToman(toman - 300_000), Now.AddMinutes(-minutesAgo), "manual");

    [Fact]
    public void FreshRate_IsOk() =>
        Assert.True(RateGuard.Demo.CanSellOnline(Rate(25_895_000, 5), Rate(25_800_000, 60), Now));

    [Fact]
    public void StaleRate_StopsOnlineSales() =>
        Assert.Equal(RateHealth.Stale, RateGuard.Demo.Check(Rate(25_895_000, 31), null, Now));

    [Fact]
    public void AbnormalJump_StopsOnlineSales() =>
        Assert.Equal(RateHealth.AbnormalJump,
            RateGuard.Demo.Check(Rate(28_000_000, 1), Rate(25_895_000, 60), Now));

    [Fact]
    public void BuyRateAboveSell_Throws() =>
        Assert.Throws<DomainException>(() =>
            GoldRate.Create(Money.FromRial(100), Money.FromRial(200), Now, "manual"));
}
