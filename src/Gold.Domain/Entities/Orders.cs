using Gold.Domain.Common;

namespace Gold.Domain.Entities;

/// <summary>Frozen price for a set of pieces, valid for a short time (default 10 minutes).</summary>
public sealed class PriceQuote : Entity
{
    public Guid UserId { get; private set; }
    public long TotalRial { get; private set; }
    public long RateSellPerGram18Rial { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>Full transparent breakdown per piece (JSON) for the invoice and audit.</summary>
    public string BreakdownJson { get; private set; } = "[]";
    public string PieceIdsCsv { get; private set; } = "";
    public bool Consumed { get; private set; }

    private PriceQuote() { }

    public static PriceQuote Create(Guid userId, IReadOnlyCollection<Guid> pieceIds, long totalRial, long rate,
        string breakdownJson, DateTimeOffset expiresAt) => new()
    {
        UserId = userId,
        PieceIdsCsv = string.Join(',', pieceIds),
        TotalRial = totalRial,
        RateSellPerGram18Rial = rate,
        BreakdownJson = breakdownJson,
        ExpiresAt = expiresAt,
    };

    public IReadOnlyList<Guid> PieceIds => PieceIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();

    public bool IsValid(DateTimeOffset now) => !Consumed && now < ExpiresAt;

    public void Consume(DateTimeOffset now)
    {
        if (!IsValid(now)) throw new DomainException("Price quote has expired. Please recalculate.");
        Consumed = true;
    }
}

public sealed class Order : Entity, IConcurrencyStamped
{
    public string Number { get; private set; } = default!;
    public Guid UserId { get; private set; }
    public Guid QuoteId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.AwaitingPayment;
    public long TotalRial { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    private Order() { }

    public static Order FromQuote(PriceQuote quote, string number, IEnumerable<(Guid PieceId, long PriceRial)> items)
    {
        var o = new Order { Number = number, UserId = quote.UserId, QuoteId = quote.Id, TotalRial = quote.TotalRial };
        foreach (var (pieceId, price) in items) o._items.Add(new OrderItem(o.Id, pieceId, price));
        if (o._items.Count == 0) throw new DomainException("Order must contain at least one piece.");
        return o;
    }

    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.AwaitingPayment] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Packed, OrderStatus.Refunded],
        [OrderStatus.Packed] = [OrderStatus.Shipped, OrderStatus.PickedUp],
        [OrderStatus.Shipped] = [OrderStatus.Delivered],
        [OrderStatus.PickedUp] = [OrderStatus.Delivered],
    };

    public bool CanMoveTo(OrderStatus next) => Allowed.TryGetValue(Status, out var n) && n.Contains(next);

    public void MoveTo(OrderStatus next)
    {
        if (!CanMoveTo(next)) throw new DomainException($"Illegal order transition {Status} → {next}.");
        Status = next;
        foreach (var i in _items) i.SetActive(next is not (OrderStatus.Cancelled or OrderStatus.Refunded));
        ConcurrencyStamp = Guid.NewGuid();
    }
}

public sealed class OrderItem
{
    public long Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid PieceId { get; private set; }
    public long PriceRial { get; private set; }
    /// <summary>Unique filtered index (PieceId where IsActive) guarantees one active order per piece in the DB.</summary>
    public bool IsActive { get; private set; } = true;

    private OrderItem() { }

    internal OrderItem(Guid orderId, Guid pieceId, long priceRial)
    {
        OrderId = orderId;
        PieceId = pieceId;
        PriceRial = priceRial;
    }

    internal void SetActive(bool active) => IsActive = active;
}

public sealed class Payment : Entity, IConcurrencyStamped
{
    public Guid OrderId { get; private set; }
    public long AmountRial { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string Gateway { get; private set; } = default!;
    public string IdempotencyKey { get; private set; } = default!;
    public string? GatewayToken { get; private set; }
    public string? TrackingCode { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.NewGuid();

    private Payment() { }

    public static Payment Start(Guid orderId, long amountRial, string gateway, string idempotencyKey) => new()
    {
        OrderId = orderId, AmountRial = amountRial, Gateway = gateway, IdempotencyKey = idempotencyKey,
    };

    public void AttachToken(string token) { GatewayToken = token; ConcurrencyStamp = Guid.NewGuid(); }

    public void Succeed(string trackingCode)
    {
        if (Status != PaymentStatus.Pending) throw new DomainException("Payment already processed.");
        Status = PaymentStatus.Succeeded;
        TrackingCode = trackingCode;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void Fail()
    {
        if (Status != PaymentStatus.Pending) throw new DomainException("Payment already processed.");
        Status = PaymentStatus.Failed;
        ConcurrencyStamp = Guid.NewGuid();
    }
}

/// <summary>Append-only rial ledger. Balances are always computed from here.</summary>
public sealed class CashLedgerEntry
{
    public long Id { get; private set; }
    public DateTimeOffset At { get; private set; }
    public LedgerDirection Direction { get; private set; }
    public long AmountRial { get; private set; }
    public string Reason { get; private set; } = default!;
    public Guid? OrderId { get; private set; }
    public Guid? PaymentId { get; private set; }

    private CashLedgerEntry() { }

    public static CashLedgerEntry Create(DateTimeOffset at, LedgerDirection dir, long amount, string reason,
        Guid? orderId, Guid? paymentId)
    {
        if (amount <= 0) throw new DomainException("Ledger amount must be positive.");
        return new() { At = at, Direction = dir, AmountRial = amount, Reason = reason, OrderId = orderId, PaymentId = paymentId };
    }
}

public sealed class AuditLog
{
    public long Id { get; private set; }
    public DateTimeOffset At { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = default!;
    public string Entity { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string Details { get; private set; } = "";
    public bool IsDemo { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(DateTimeOffset at, Guid? userId, string action, string entity, string entityId,
        string details = "", bool isDemo = false) =>
        new() { At = at, UserId = userId, Action = action, Entity = entity, EntityId = entityId, Details = details, IsDemo = isDemo };
}

/// <summary>Transactional outbox for SMS/notifications; dispatched by a background worker.</summary>
public sealed class OutboxMessage
{
    public long Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(DateTimeOffset at, string type, string payload) =>
        new() { CreatedAt = at, Type = type, Payload = payload };

    public void MarkProcessed(DateTimeOffset at) => ProcessedAt = at;
    public void RegisterFailure() => Attempts++;
}

/// <summary>
/// White-label site content: one JSON document per section key (brand, theme, contact, home, ...).
/// Everything a customer sees on the site comes from here — nothing is hard-coded.
/// </summary>
public sealed class SiteSetting
{
    public string Key { get; private set; } = default!;
    public string Json { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    private SiteSetting() { }

    public static SiteSetting Create(string key, string json, DateTimeOffset at, Guid? by) =>
        new() { Key = key, Json = json, UpdatedAt = at, UpdatedByUserId = by };

    public void Update(string json, DateTimeOffset at, Guid? by) { Json = json; UpdatedAt = at; UpdatedByUserId = by; }
}
