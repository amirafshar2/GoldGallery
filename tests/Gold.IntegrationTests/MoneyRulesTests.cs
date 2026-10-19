using System.Net;
using System.Net.Http.Json;
using Gold.Infrastructure.Services;

namespace Gold.IntegrationTests;

public class MoneyRulesTests : IDisposable
{
    private readonly TestApp _app = new();
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task ConcurrentOrders_SamePiece_OnlyOneSucceeds()
    {
        var a = await _app.CustomerAsync("09120000001");
        var b = await _app.CustomerAsync("09120000002");
        var piece = await a.FirstAvailablePieceAsync();

        // Both get a valid quote first, then race to create the order.
        var qa = (await (await a.PostAsJsonAsync("/api/v1/quotes", new { pieceIds = new[] { piece } })).Json()).GetProperty("id").GetString();
        var qb = (await (await b.PostAsJsonAsync("/api/v1/quotes", new { pieceIds = new[] { piece } })).Json()).GetProperty("id").GetString();

        var results = await Task.WhenAll(
            a.PostAsJsonAsync("/api/v1/orders", new { quoteId = qa }),
            b.PostAsJsonAsync("/api/v1/orders", new { quoteId = qb }));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task DuplicateCallback_IsProcessedOnce_LedgerHasOneEntry()
    {
        var c = await _app.DemoAsync("customer");
        var order = await (await c.OrderPieceAsync(await c.FirstAvailablePieceAsync())).Json();
        var (paymentId, token) = await c.StartPaymentAsync(order.GetProperty("id").GetString()!, "dup-1");
        FakePaymentGateway.Transactions[token].Outcome = "paid";

        var anon = _app.Client();
        var r1 = await (await anon.GetAsync($"/api/v1/payments/callback?paymentId={paymentId}")).Json();
        var r2 = await (await anon.GetAsync($"/api/v1/payments/callback?paymentId={paymentId}")).Json();

        Assert.Equal("Succeeded", r1.GetProperty("status").GetString());
        Assert.Equal("Succeeded", r2.GetProperty("status").GetString());
        var admin = await _app.DemoAsync("admin");
        var ledger = await (await admin.GetAsync("/api/v1/admin/ledger/summary")).Json();
        Assert.Equal(1, ledger.GetProperty("entries").GetInt32());
        Assert.Equal(order.GetProperty("totalRial").GetInt64(), ledger.GetProperty("balanceRial").GetInt64());
    }

    [Fact]
    public async Task TamperedAmount_IsRejected_AndPieceReleased()
    {
        var c = await _app.DemoAsync("customer");
        var piece = await c.FirstAvailablePieceAsync("bracelet-rope");
        var order = await (await c.OrderPieceAsync(piece)).Json();
        var (paymentId, token) = await c.StartPaymentAsync(order.GetProperty("id").GetString()!, "tamper-1");
        FakePaymentGateway.Transactions[token].Outcome = "tampered";

        var r = await (await _app.Client().GetAsync($"/api/v1/payments/callback?paymentId={paymentId}")).Json();

        Assert.Equal("Failed", r.GetProperty("status").GetString());
        var o = await (await c.GetAsync($"/api/v1/orders/{order.GetProperty("id").GetString()}")).Json();
        Assert.Equal("Cancelled", o.GetProperty("status").GetString());
        Assert.Equal(piece, await c.FirstAvailablePieceAsync("bracelet-rope")); // available again
    }

    [Fact]
    public async Task IdempotencyKey_ReturnsSamePayment()
    {
        var c = await _app.DemoAsync("customer");
        var order = await (await c.OrderPieceAsync(await c.FirstAvailablePieceAsync("chain-venetian"))).Json();
        var id = order.GetProperty("id").GetString()!;
        var p1 = await c.StartPaymentAsync(id, "same-key");
        var p2 = await c.StartPaymentAsync(id, "same-key");
        Assert.Equal(p1.PaymentId, p2.PaymentId);
    }

    [Fact]
    public async Task CustomerCannotReadAnotherCustomersOrder()
    {
        var a = await _app.CustomerAsync("09120000011");
        var b = await _app.CustomerAsync("09120000012");
        var order = await (await a.OrderPieceAsync(await a.FirstAvailablePieceAsync("bangle-classic"))).Json();
        var res = await b.GetAsync($"/api/v1/orders/{order.GetProperty("id").GetString()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task LargeRateJump_NeedsSecondConfirmation()
    {
        var admin = await _app.DemoAsync("admin");
        var jump = await admin.PostAsJsonAsync("/api/v1/admin/rates", new { sellPerGram18Rial = 400_000_000L, buyPerGram18Rial = 390_000_000L });
        Assert.Equal(HttpStatusCode.Conflict, jump.StatusCode);
        var ok = await admin.PostAsJsonAsync("/api/v1/admin/rates", new { sellPerGram18Rial = 400_000_000L, buyPerGram18Rial = 390_000_000L, confirmLargeJump = true });
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
    }
}

public class StaleRateTests : IDisposable
{
    private readonly TestApp _app = new(new() { ["RateGuard:MaxAgeMinutes"] = "0" });
    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task StaleRate_StopsOnlineSales()
    {
        var c = await _app.DemoAsync("customer");
        var res = await c.PostAsJsonAsync("/api/v1/quotes", new { pieceIds = new[] { await c.FirstAvailablePieceAsync() } });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, res.StatusCode);
    }
}
