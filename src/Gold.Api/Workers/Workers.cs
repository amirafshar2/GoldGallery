using Gold.Application.Orders;
using Gold.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gold.Api.Workers;

/// <summary>Cancels unpaid orders after the payment window and releases reserved pieces. (Hangfire later.)</summary>
public sealed class OrderExpiryWorker(IServiceScopeFactory scopes, ILogger<OrderExpiryWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<OrderService>().ExpireUnpaidOrdersAsync(ct);
                if (n > 0) log.LogInformation("Expired {Count} unpaid orders", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Order expiry failed"); }
        }
    }
}

/// <summary>Dispatches the transactional outbox (SMS/notifications). Dev: logs the message.</summary>
public sealed class OutboxDispatcher(IServiceScopeFactory scopes, ILogger<OutboxDispatcher> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var batch = await db.Outbox.Where(o => o.ProcessedAt == null && o.Attempts < 5).OrderBy(o => o.Id).Take(20).ToListAsync(ct);
                foreach (var m in batch)
                {
                    log.LogInformation("OUTBOX {Type}: {Payload}", m.Type, m.Payload);
                    m.MarkProcessed(DateTimeOffset.UtcNow);
                }
                if (batch.Count > 0) await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Outbox dispatch failed"); }
        }
    }
}

/// <summary>
/// Public showcase only: visitors may log in as the demo admin and change anything, so every few hours
/// the SQLite demo database is rebuilt from the seed. Never registered outside Showcase mode.
/// </summary>
public sealed class ShowcaseResetWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<ShowcaseResetWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var hours = Math.Clamp(config.GetValue("Showcase:ResetHours", 6), 1, 168);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (!db.Database.IsSqlite()) { log.LogWarning("Showcase reset skipped: only SQLite demo databases are reset."); return; }
                await db.Database.EnsureDeletedAsync(ct);
                await Gold.Api.Infrastructure.DevDatabase.EnsureCurrentAsync(db, log);
                await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(config["Seed:Profile"], ct);
                log.LogInformation("Showcase demo data was reset.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Showcase reset failed"); }
        }
    }
}
