using System.Net;
using Gold.Api.Infrastructure;
using Gold.Api.Security;
using Gold.Application.Orders;
using Gold.Application.Payments;
using Gold.Infrastructure.Services;

namespace Gold.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrders(this RouteGroupBuilder api)
    {
        api.MapPost("/buyback/estimate", async (BuyBackRequest req, OrderService svc, CancellationToken ct) =>
            (await svc.EstimateBuyBackAsync(req, ct)).ToHttp()).Validate<BuyBackRequest>().WithTags("Old gold");

        var g = api.MapGroup("").WithTags("Orders").RequireAuthorization(Policies.Customer);

        g.MapPost("/quotes", async (CreateQuoteRequest req, OrderService svc, CancellationToken ct) =>
            (await svc.CreateQuoteAsync(req, ct)).ToHttp(q => Results.Created($"/api/v1/quotes/{q.Id}", q)))
            .Validate<CreateQuoteRequest>();

        g.MapPost("/orders", async (CreateOrderRequest req, OrderService svc, CancellationToken ct) =>
            (await svc.CreateOrderAsync(req, ct)).ToHttp(o => Results.Created($"/api/v1/orders/{o.Id}", o)));
        g.MapGet("/orders", async (OrderService svc, CancellationToken ct) => Results.Ok(await svc.MyOrdersAsync(ct)));
        g.MapGet("/orders/{id:guid}", async (Guid id, OrderService svc, CancellationToken ct) => (await svc.GetAsync(id, ct)).ToHttp());

        g.MapPost("/orders/{id:guid}/payments", async (Guid id, HttpContext http, PaymentService svc, IConfiguration cfg, CancellationToken ct) =>
        {
            var key = http.Request.Headers["Idempotency-Key"].ToString();
            // Real gateways need an absolute public URL (Payment:CallbackBaseUrl); dev uses a relative path through the web proxy.
            var callback = (cfg["Payment:CallbackBaseUrl"] ?? "") + "/api/v1/payments/callback";
            return (await svc.StartAsync(id, key, callback, ct)).ToHttp(r => Results.Created($"/api/v1/payments/{r.PaymentId}", r));
        }).RequireRateLimiting("payment");

        // Gateway callback (GET/POST). Anonymous: the gateway (not the user) calls it; state is verified server-side.
        api.MapMethods("/payments/callback", ["GET", "POST"], async (Guid paymentId, HttpContext http, PaymentService svc, IConfiguration cfg, CancellationToken ct) =>
        {
            var r = await svc.HandleCallbackAsync(paymentId, ct);
            var resultPage = cfg["Payment:ResultPageUrl"];
            var fromBrowser = http.Request.Headers.Accept.ToString().Contains("text/html");
            if (r.IsSuccess && fromBrowser && !string.IsNullOrEmpty(resultPage))
                return Results.Redirect($"{resultPage}?paymentId={r.Value!.PaymentId}&orderId={r.Value.OrderId}&status={r.Value.Status}");
            return r.ToHttp();
        }).WithTags("Orders").RequireRateLimiting("payment");

        var staff = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Staff);
        staff.MapGet("/orders", async (OrderService svc, CancellationToken ct) => Results.Ok(await svc.AllOrdersAsync(ct)));
        staff.MapPut("/orders/{id:guid}/status", async (Guid id, ChangeOrderStatusRequest req, OrderService svc, CancellationToken ct) =>
            (await svc.ChangeStatusAsync(id, req, ct)).ToHttp());
        staff.MapGet("/ledger/summary", async (PaymentService svc, CancellationToken ct) => Results.Ok(await svc.LedgerSummaryAsync(ct)))
            .RequireAuthorization(Policies.Admin);
        return api;
    }

    /// <summary>Development-only sandbox gateway page with Pay / Cancel / Tamper buttons.</summary>
    public static void MapFakeGateway(this WebApplication app, bool exposeOtp = true)
    {
        // Development only: lets automated visual tests read the OTP that LogSmsSender "sent".
        // Never on a public showcase — anyone could read anyone's code.
        if (exposeOtp)
            app.MapGet("/dev/last-otp", (string phone) =>
                LogSmsSender.LastMessages.TryGetValue(phone, out var m)
                    ? Results.Text(new string(m.Where(char.IsDigit).ToArray()))
                    : Results.NotFound());

        app.MapGet("/dev/fake-gateway", (string token) =>
        {
            if (!FakePaymentGateway.Transactions.TryGetValue(token, out var tx)) return Results.NotFound();
            var t = WebUtility.HtmlEncode(token);
            var html = $$"""
                <!doctype html><html lang="fa" dir="rtl"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                <title>درگاه آزمایشی</title>
                <body style="font-family:system-ui;background:#0B0A08;color:#F4EFE6;display:grid;place-items:center;min-height:100vh;margin:0">
                <main style="background:#15130f;border:1px solid #C9A24A;border-radius:16px;padding:24px;max-width:360px;width:calc(100% - 32px)">
                <h1 style="color:#e8c877;font-size:20px">درگاه پرداخت آزمایشی</h1>
                <p>مبلغ: <b id="amount">{{tx.Amount / 10:N0}}</b> تومان</p>
                <form method="post" action="/dev/fake-gateway/{{t}}/paid"><button id="pay" style="width:100%;padding:12px;margin:6px 0;background:#C9A24A;border:0;border-radius:10px">پرداخت موفق</button></form>
                <form method="post" action="/dev/fake-gateway/{{t}}/cancelled"><button id="cancel" style="width:100%;padding:12px;margin:6px 0;border-radius:10px">انصراف</button></form>
                <form method="post" action="/dev/fake-gateway/{{t}}/tampered"><button id="tamper" style="width:100%;padding:12px;margin:6px 0;border-radius:10px">شبیه‌سازی مبلغ دستکاری‌شده</button></form>
                </main></body></html>
                """;
            return Results.Content(html, "text/html; charset=utf-8");
        });
        app.MapPost("/dev/fake-gateway/{token}/{outcome}", (string token, string outcome) =>
        {
            if (!FakePaymentGateway.Transactions.TryGetValue(token, out var tx) || outcome is not ("paid" or "cancelled" or "tampered"))
                return Results.NotFound();
            tx.Outcome = outcome;
            return Results.Redirect(tx.CallbackUrl);
        }).DisableAntiforgery();
    }
}
