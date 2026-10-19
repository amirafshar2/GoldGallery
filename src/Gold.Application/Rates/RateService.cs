using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Rates;

public sealed record RateDto(long SellPerGram18Rial, long BuyPerGram18Rial, DateTimeOffset AsOf, string Source,
    string Health, bool CanSellOnline);

public sealed record RatePointDto(DateTimeOffset AsOf, long SellPerGram18Rial, long BuyPerGram18Rial);

public sealed record SetRateRequest(long SellPerGram18Rial, long BuyPerGram18Rial, bool ConfirmLargeJump = false);

public sealed record PricingSettingsDto(int ProfitBp, int TaxBp, long RoundingIncrementRial, RoundingDirection RoundingDirection, DateTimeOffset ValidFrom);

public sealed record SetPricingSettingsRequest(int ProfitBp, int TaxBp, long RoundingIncrementRial, RoundingDirection RoundingDirection);

public sealed class RateService(IAppDbContext db, IClock clock, ICurrentUser user, IRateNotifier notifier, RateGuard guard)
{
    public async Task<(GoldRate? Current, GoldRate? Previous)> GetLatestTwoAsync(CancellationToken ct)
    {
        var two = await db.GoldRates.AsNoTracking().OrderByDescending(r => r.Id).Take(2).ToListAsync(ct);
        return (two.ElementAtOrDefault(0)?.ToRate(), two.ElementAtOrDefault(1)?.ToRate());
    }

    public async Task<Result<RateDto>> GetCurrentAsync(CancellationToken ct)
    {
        var (cur, prev) = await GetLatestTwoAsync(ct);
        if (cur is null) return Error.Unavailable("rate.missing", "نرخ روز هنوز ثبت نشده است.");
        return ToDto(cur, guard.Check(cur, prev, clock.Now));
    }

    public async Task<IReadOnlyList<RatePointDto>> GetHistoryAsync(int days, CancellationToken ct)
    {
        var from = clock.Now.AddDays(-Math.Clamp(days, 1, 365));
        var rows = await db.GoldRates.AsNoTracking()
            .OrderBy(r => r.Id)
            .Select(r => new RatePointDto(r.AsOf, r.SellPerGram18Rial, r.BuyPerGram18Rial))
            .ToListAsync(ct);
        return rows.Where(r => r.AsOf >= from).ToList(); // DateTimeOffset filtering done in memory for SQLite compatibility
    }

    public async Task<Result<RateDto>> SetAsync(SetRateRequest req, CancellationToken ct)
    {
        GoldRate rate;
        try { rate = GoldRate.Create(Money.FromRial(req.SellPerGram18Rial), Money.FromRial(req.BuyPerGram18Rial), clock.Now, "manual"); }
        catch (Gold.Domain.Common.DomainException ex) { return Error.Validation("rate.invalid", ex.Message); }

        var (cur, _) = await GetLatestTwoAsync(ct);
        if (cur is not null && guard.Check(rate, cur, clock.Now) == RateHealth.AbnormalJump && !req.ConfirmLargeJump)
            return Error.Conflict("rate.jump", "تغییر نرخ بیش از حد مجاز است؛ برای ثبت، تأیید دوم لازم است (ConfirmLargeJump).");

        db.GoldRates.Add(GoldRateEntry.From(rate, user.Id));
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, "rate.set", "GoldRate", "-",
            $"sell={req.SellPerGram18Rial};buy={req.BuyPerGram18Rial};confirmJump={req.ConfirmLargeJump}", user.IsDemo));
        await db.SaveChangesAsync(ct);

        // A confirmed big jump is accepted: health is evaluated against itself only.
        var dto = ToDto(rate, guard.Check(rate, req.ConfirmLargeJump ? null : cur, clock.Now));
        await notifier.RateChangedAsync(dto, ct);
        return dto;
    }

    public async Task<PricingPolicy> GetPolicyAsync(CancellationToken ct)
    {
        var now = clock.Now;
        var all = await db.PricingSettings.AsNoTracking().OrderByDescending(s => s.Id).ToListAsync(ct);
        return all.FirstOrDefault(s => s.ValidFrom <= now)?.ToPolicy() ?? PricingPolicy.Demo;
    }

    public async Task<PricingSettingsDto> GetSettingsAsync(CancellationToken ct)
    {
        var p = await GetPolicyAsync(ct);
        return new PricingSettingsDto(p.Profit.BasisPoints, p.Tax.BasisPoints, p.Rounding.IncrementRial, p.Rounding.Direction, clock.Now);
    }

    public async Task<Result<PricingSettingsDto>> SetSettingsAsync(SetPricingSettingsRequest req, CancellationToken ct)
    {
        PricingPolicy policy;
        try
        {
            policy = new PricingPolicy(Percentage.FromBasisPoints(req.ProfitBp), Percentage.FromBasisPoints(req.TaxBp),
                new RoundingPolicy(req.RoundingIncrementRial, req.RoundingDirection));
        }
        catch (Gold.Domain.Common.DomainException ex) { return Error.Validation("pricing.invalid", ex.Message); }

        db.PricingSettings.Add(Domain.Entities.PricingSettings.Create(policy, clock.Now, user.Id));
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, "pricing.set", "PricingSettings", "-",
            $"profit={req.ProfitBp};tax={req.TaxBp};round={req.RoundingIncrementRial}/{req.RoundingDirection}", user.IsDemo));
        await db.SaveChangesAsync(ct);
        return new PricingSettingsDto(req.ProfitBp, req.TaxBp, req.RoundingIncrementRial, req.RoundingDirection, clock.Now);
    }

    private static RateDto ToDto(GoldRate r, RateHealth h) =>
        new(r.SellPerGram18.Rial, r.BuyPerGram18.Rial, r.AsOf, r.Source, h.ToString(), h == RateHealth.Ok);
}
