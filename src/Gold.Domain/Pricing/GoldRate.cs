using Gold.Domain.Common;
using Gold.Domain.ValueObjects;

namespace Gold.Domain.Pricing;

/// <summary>A daily/hourly rate snapshot for 18K gold (sell = ask, buy = bid).</summary>
public sealed record GoldRate(Money SellPerGram18, Money BuyPerGram18, DateTimeOffset AsOf, string Source)
{
    public static GoldRate Create(Money sell, Money buy, DateTimeOffset asOf, string source)
    {
        if (sell.Rial <= 0 || buy.Rial <= 0) throw new DomainException("Rates must be positive.");
        if (buy.Rial > sell.Rial) throw new DomainException("Buy rate cannot exceed sell rate.");
        return new GoldRate(sell, buy, asOf, source);
    }
}

public enum RateHealth
{
    Ok = 0,
    /// <summary>Rate older than allowed — online sales must stop.</summary>
    Stale = 1,
    /// <summary>Rate jumped more than allowed vs previous — needs second approval; sales stop.</summary>
    AbnormalJump = 2,
}

/// <summary>Staleness guard: never sell with an outdated or suspicious rate.</summary>
public sealed record RateGuard(TimeSpan MaxAge, Percentage MaxJump)
{
    public static RateGuard Demo => new(TimeSpan.FromMinutes(30), Percentage.FromPercent(5));

    public RateHealth Check(GoldRate current, GoldRate? previous, DateTimeOffset now)
    {
        if (now - current.AsOf > MaxAge) return RateHealth.Stale;
        if (previous is not null)
        {
            var diff = Math.Abs(current.SellPerGram18.Rial - previous.SellPerGram18.Rial);
            if (diff > MaxJump.Of(previous.SellPerGram18.Rial)) return RateHealth.AbnormalJump;
        }
        return RateHealth.Ok;
    }

    public bool CanSellOnline(GoldRate current, GoldRate? previous, DateTimeOffset now) =>
        Check(current, previous, now) == RateHealth.Ok;
}
