using System.Text.Json;
using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Application.Catalog;
using Gold.Application.Rates;
using Gold.Domain.Common;
using Gold.Domain.Entities;
using Gold.Domain.Pricing;
using Gold.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Orders;

public sealed record CreateQuoteRequest(IReadOnlyList<Guid> PieceIds);
public sealed record QuoteLineDto(Guid PieceId, string ProductName, decimal GoldGrams, int Karat, long RawGoldRial,
    long WageRial, long ProfitRial, long TaxRial, long StoneValueRial, long RoundingAdjustmentRial, long TotalRial);
public sealed record QuoteDto(Guid Id, long TotalRial, long RateSellPerGram18Rial, DateTimeOffset ExpiresAt, IReadOnlyList<QuoteLineDto> Lines);
public sealed record CreateOrderRequest(Guid QuoteId);
public sealed record OrderItemDto(Guid PieceId, string ProductName, decimal GoldGrams, long PriceRial);
public sealed record OrderDto(Guid Id, string Number, string Status, long TotalRial, DateTimeOffset CreatedAt, IReadOnlyList<OrderItemDto> Items);
public sealed record ChangeOrderStatusRequest(OrderStatus Status);
public sealed record BuyBackRequest(decimal? Grams, decimal? Mesghal, int Karat);
public sealed record BuyBackDto(decimal Grams, decimal Mesghal, int Karat, long BuyRatePerGram18Rial, long ValueRial);

public sealed class OrderService(IAppDbContext db, IClock clock, ICurrentUser user, RateService rates, RateGuard guard)
{
    public static readonly TimeSpan QuoteLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan PaymentWindow = TimeSpan.FromMinutes(15);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<Result<QuoteDto>> CreateQuoteAsync(CreateQuoteRequest req, CancellationToken ct)
    {
        if (user.Id is not { } userId) return Error.Unauthorized("auth.required", "ابتدا وارد شوید.");
        var ids = req.PieceIds.Distinct().ToList();
        if (ids.Count is 0 or > 10) return Error.Validation("quote.pieces", "بین ۱ تا ۱۰ قطعه انتخاب کنید.");

        var (rate, prev) = await rates.GetLatestTwoAsync(ct);
        if (rate is null || !guard.CanSellOnline(rate, prev, clock.Now))
            return Error.Unavailable("rate.unhealthy", "نرخ طلا در حال به‌روزرسانی است؛ فروش آنلاین موقتاً متوقف است. لطفاً تماس بگیرید یا رزرو حضوری کنید.");

        var pieces = await db.Pieces.AsNoTracking().Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Status, p.GoldWeightMg, p.KaratPerMille, p.StoneValueRial, ProductName = p.Product!.Name, p.Product.WageKind, p.Product.WageValue })
            .ToListAsync(ct);
        if (pieces.Count != ids.Count) return Error.NotFound("quote.pieceNotFound", "یکی از قطعه‌ها پیدا نشد.");
        if (pieces.Any(p => p.Status != PieceStatus.Available)) return Error.Conflict("quote.pieceUnavailable", "یکی از قطعه‌ها دیگر موجود نیست.");

        var policy = await rates.GetPolicyAsync(ct);
        var lines = pieces.Select(p =>
        {
            var b = PricingEngine.CalculateSale(Weight.FromMilligrams(p.GoldWeightMg), Karat.FromPerMille(p.KaratPerMille),
                rate.SellPerGram18, CatalogService.ToWage(p.WageKind, p.WageValue), policy, Money.FromRial(p.StoneValueRial));
            return new QuoteLineDto(p.Id, p.ProductName, p.GoldWeightMg / 1000m, p.KaratPerMille, b.RawGold.Rial, b.Wage.Rial,
                b.Profit.Rial, b.Tax.Rial, b.StoneValue.Rial, b.RoundingAdjustmentRial, b.Total.Rial);
        }).ToList();

        var total = lines.Sum(l => l.TotalRial);
        var quote = PriceQuote.Create(userId, ids, total, rate.SellPerGram18.Rial, JsonSerializer.Serialize(lines, Json), clock.Now.Add(QuoteLifetime));
        db.PriceQuotes.Add(quote);
        await db.SaveChangesAsync(ct);
        return new QuoteDto(quote.Id, total, rate.SellPerGram18.Rial, quote.ExpiresAt, lines);
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CreateOrderRequest req, CancellationToken ct)
    {
        if (user.Id is not { } userId) return Error.Unauthorized("auth.required", "ابتدا وارد شوید.");
        var quote = await db.PriceQuotes.FirstOrDefaultAsync(q => q.Id == req.QuoteId && q.UserId == userId, ct);
        if (quote is null) return Error.NotFound("quote.notFound", "پیش‌فاکتور پیدا نشد.");

        try { quote.Consume(clock.Now); }
        catch (DomainException ex) { return Error.Conflict("quote.expired", ex.Message); }

        var lines = JsonSerializer.Deserialize<List<QuoteLineDto>>(quote.BreakdownJson, Json)!;
        var pieces = await db.Pieces.Where(p => quote.PieceIds.Contains(p.Id)).ToListAsync(ct);
        try { pieces.ForEach(p => p.Reserve()); }
        catch (DomainException ex) { return Error.Conflict("piece.taken", "این قطعه همین الان توسط مشتری دیگری رزرو شد. " + ex.Message); }

        var number = $"G-{clock.Now:yyMMdd}-{Random.Shared.Next(100000, 999999)}";
        var order = Order.FromQuote(quote, number, lines.Select(l => (l.PieceId, l.TotalRial)));
        db.Orders.Add(order);
        db.AuditLogs.Add(AuditLog.Create(clock.Now, userId, "order.create", "Order", order.Id.ToString(), $"total={order.TotalRial}", user.IsDemo));

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) // concurrency stamp mismatch or unique active-piece index violation
        {
            return Error.Conflict("piece.taken", "این قطعه همین الان توسط مشتری دیگری رزرو شد.");
        }
        return ToDto(order, lines.ToDictionary(l => l.PieceId));
    }

    public async Task<IReadOnlyList<OrderDto>> MyOrdersAsync(CancellationToken ct)
    {
        if (user.Id is not { } userId) return [];
        return await ListAsync(db.Orders.Where(o => o.UserId == userId), ct);
    }

    public Task<IReadOnlyList<OrderDto>> AllOrdersAsync(CancellationToken ct) => ListAsync(db.Orders, ct);

    public async Task<Result<OrderDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var q = db.Orders.Where(o => o.Id == id);
        if (user.Role is UserRole.Customer or UserRole.Wholesale) q = q.Where(o => o.UserId == user.Id); // IDOR guard
        var list = await ListAsync(q, ct);
        return list.Count == 0 ? Error.NotFound("order.notFound", "سفارش پیدا نشد.") : list[0];
    }

    public async Task<Result<OrderDto>> ChangeStatusAsync(Guid id, ChangeOrderStatusRequest req, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Error.NotFound("order.notFound", "سفارش پیدا نشد.");
        if (req.Status is OrderStatus.Paid) return Error.Validation("order.paidByGateway", "وضعیت «پرداخت‌شده» فقط از طریق تأیید درگاه ثبت می‌شود.");
        var from = order.Status;
        try { order.MoveTo(req.Status); }
        catch (DomainException ex) { return Error.Conflict("order.transition", ex.Message); }

        if (req.Status is OrderStatus.Cancelled)
        {
            var pieces = await db.Pieces.Where(p => order.Items.Select(i => i.PieceId).Contains(p.Id)).ToListAsync(ct);
            pieces.ForEach(p => p.Release());
        }
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, "order.status", "Order", id.ToString(), $"{from}->{req.Status}", user.IsDemo));
        await db.SaveChangesAsync(ct);
        return (await GetAsync(id, ct)).Value!;
    }

    /// <summary>Background job: cancel unpaid orders after the payment window and release their pieces.</summary>
    public async Task<int> ExpireUnpaidOrdersAsync(CancellationToken ct)
    {
        var limit = clock.Now - PaymentWindow;
        var candidates = await db.Orders.Include(o => o.Items).Where(o => o.Status == OrderStatus.AwaitingPayment).ToListAsync(ct);
        var stale = candidates.Where(o => o.CreatedAt < limit).ToList();
        foreach (var o in stale)
        {
            o.MoveTo(OrderStatus.Cancelled);
            var ids = o.Items.Select(i => i.PieceId).ToList();
            (await db.Pieces.Where(p => ids.Contains(p.Id)).ToListAsync(ct)).ForEach(p => p.Release());
            db.AuditLogs.Add(AuditLog.Create(clock.Now, null, "order.expired", "Order", o.Id.ToString()));
        }
        if (stale.Count > 0) await db.SaveChangesAsync(ct);
        return stale.Count;
    }

    public async Task<Result<BuyBackDto>> EstimateBuyBackAsync(BuyBackRequest req, CancellationToken ct)
    {
        var (rate, _) = await rates.GetLatestTwoAsync(ct);
        if (rate is null) return Error.Unavailable("rate.missing", "نرخ روز هنوز ثبت نشده است.");
        try
        {
            var w = req.Mesghal is { } m ? Weight.FromMesghal(m) : Weight.FromGrams(req.Grams ?? 0);
            var v = PricingEngine.CalculateBuyBack(w, Karat.FromPerMille(req.Karat), rate.BuyPerGram18);
            return new BuyBackDto(w.Grams, decimal.Round(w.Mesghal, 3), req.Karat, rate.BuyPerGram18.Rial, v.Value.Rial);
        }
        catch (DomainException ex) { return Error.Validation("buyback.invalid", ex.Message); }
    }

    private async Task<IReadOnlyList<OrderDto>> ListAsync(IQueryable<Order> q, CancellationToken ct)
    {
        var rows = await q.AsNoTracking().OrderByDescending(o => o.CreatedAt).Take(200)
            .Select(o => new
            {
                o.Id, o.Number, o.Status, o.TotalRial, o.CreatedAt,
                Items = o.Items.Select(i => new { i.PieceId, i.PriceRial }).ToList(),
            }).ToListAsync(ct);
        var pieceIds = rows.SelectMany(r => r.Items.Select(i => i.PieceId)).Distinct().ToList();
        var info = await db.Pieces.IgnoreQueryFilters().AsNoTracking().Where(p => pieceIds.Contains(p.Id))
            .Select(p => new { p.Id, Name = p.Product!.Name, p.GoldWeightMg }).ToDictionaryAsync(p => p.Id, ct);
        return rows.Select(r => new OrderDto(r.Id, r.Number, r.Status.ToString(), r.TotalRial, r.CreatedAt,
            r.Items.Select(i => new OrderItemDto(i.PieceId, info.GetValueOrDefault(i.PieceId)?.Name ?? "-",
                (info.GetValueOrDefault(i.PieceId)?.GoldWeightMg ?? 0) / 1000m, i.PriceRial)).ToList())).ToList();
    }

    private OrderDto ToDto(Order o, Dictionary<Guid, QuoteLineDto> lines) =>
        new(o.Id, o.Number, o.Status.ToString(), o.TotalRial, clock.Now,
            o.Items.Select(i => new OrderItemDto(i.PieceId, lines[i.PieceId].ProductName, lines[i.PieceId].GoldGrams, i.PriceRial)).ToList());
}
