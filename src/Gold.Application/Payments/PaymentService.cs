using System.Text.Json;
using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Payments;

public sealed record StartPaymentResult(Guid PaymentId, string RedirectUrl, long AmountRial);
public sealed record PaymentOutcome(Guid PaymentId, Guid OrderId, string Status, string? TrackingCode);
public sealed record LedgerSummaryDto(long InRial, long OutRial, long BalanceRial, int Entries);

public sealed class PaymentService(IAppDbContext db, IClock clock, ICurrentUser user, IPaymentGateway gateway)
{
    public async Task<Result<StartPaymentResult>> StartAsync(Guid orderId, string? idempotencyKey, string callbackBaseUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            return Error.Validation("payment.idempotencyKey", "هدر Idempotency-Key الزامی است.");

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id, ct);
        if (order is null) return Error.NotFound("order.notFound", "سفارش پیدا نشد.");

        var existing = await db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.OrderId != orderId) return Error.Conflict("payment.keyReused", "این کلید برای سفارش دیگری استفاده شده است.");
            return new StartPaymentResult(existing.Id, $"{callbackBaseUrl}?paymentId={existing.Id}&token={existing.GatewayToken}", existing.AmountRial);
        }
        if (order.Status != OrderStatus.AwaitingPayment) return Error.Conflict("order.notPayable", "این سفارش قابل پرداخت نیست.");

        // Amount ALWAYS from the database, never from the client.
        var payment = Payment.Start(order.Id, order.TotalRial, gateway.Name, idempotencyKey);
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var start = await gateway.StartAsync(payment.Id, payment.AmountRial, $"{callbackBaseUrl}?paymentId={payment.Id}", ct);
        payment.AttachToken(start.Token);
        await db.SaveChangesAsync(ct);
        return new StartPaymentResult(payment.Id, start.RedirectUrl, payment.AmountRial);
    }

    /// <summary>Gateway callback. Idempotent: a second call returns the stored outcome and changes nothing.</summary>
    public async Task<Result<PaymentOutcome>> HandleCallbackAsync(Guid paymentId, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return Error.NotFound("payment.notFound", "پرداخت پیدا نشد.");
        if (payment.Status != PaymentStatus.Pending)
            return new PaymentOutcome(payment.Id, payment.OrderId, payment.Status.ToString(), payment.TrackingCode);

        var order = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == payment.OrderId, ct);
        var verify = await gateway.VerifyAsync(payment.GatewayToken ?? "", ct);
        var pieceIds = order.Items.Select(i => i.PieceId).ToList();
        var pieces = await db.Pieces.Where(p => pieceIds.Contains(p.Id)).ToListAsync(ct);

        var amountOk = verify.AmountRial == payment.AmountRial && payment.AmountRial == order.TotalRial;
        if (verify.Success && amountOk && order.Status == OrderStatus.AwaitingPayment)
        {
            payment.Succeed(verify.TrackingCode);
            order.MoveTo(OrderStatus.Paid);
            pieces.ForEach(p => p.MarkSold());
            db.CashLedger.Add(CashLedgerEntry.Create(clock.Now, LedgerDirection.In, payment.AmountRial, "order.payment", order.Id, payment.Id));
            db.Outbox.Add(OutboxMessage.Create(clock.Now, "sms.orderPaid",
                JsonSerializer.Serialize(new { orderId = order.Id, order.Number, payment.TrackingCode })));
            db.AuditLogs.Add(AuditLog.Create(clock.Now, order.UserId, "payment.succeeded", "Payment", payment.Id.ToString(), $"amount={payment.AmountRial}"));
        }
        else
        {
            payment.Fail();
            if (order.CanMoveTo(OrderStatus.Cancelled)) order.MoveTo(OrderStatus.Cancelled);
            pieces.ForEach(p => p.Release());
            db.AuditLogs.Add(AuditLog.Create(clock.Now, order.UserId, amountOk ? "payment.failed" : "payment.amountMismatch",
                "Payment", payment.Id.ToString(), $"expected={payment.AmountRial};gateway={verify.AmountRial}"));
        }

        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            // A parallel duplicate callback won the race; return whatever is stored now.
            var fresh = await db.Payments.AsNoTracking().FirstAsync(p => p.Id == paymentId, ct);
            return new PaymentOutcome(fresh.Id, fresh.OrderId, fresh.Status.ToString(), fresh.TrackingCode);
        }
        return new PaymentOutcome(payment.Id, payment.OrderId, payment.Status.ToString(), payment.TrackingCode);
    }

    public async Task<LedgerSummaryDto> LedgerSummaryAsync(CancellationToken ct)
    {
        var rows = await db.CashLedger.AsNoTracking().Select(l => new { l.Direction, l.AmountRial }).ToListAsync(ct);
        var inn = rows.Where(r => r.Direction == LedgerDirection.In).Sum(r => r.AmountRial);
        var outt = rows.Where(r => r.Direction == LedgerDirection.Out).Sum(r => r.AmountRial);
        return new LedgerSummaryDto(inn, outt, inn - outt, rows.Count);
    }
}
