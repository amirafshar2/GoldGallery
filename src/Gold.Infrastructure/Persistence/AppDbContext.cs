using Gold.Application.Abstractions;
using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Gold.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IClock clock) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Piece> Pieces => Set<Piece>();
    public DbSet<GoldRateEntry> GoldRates => Set<GoldRateEntry>();
    public DbSet<PricingSettings> PricingSettings => Set<PricingSettings>();
    public DbSet<PriceQuote> PriceQuotes => Set<PriceQuote>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashLedgerEntry> CashLedger => Set<CashLedgerEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder b)
    {
        // SQLite cannot order/compare DateTimeOffset natively; store as UTC ticks there.
        if (Database.IsSqlite())
        {
            b.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
            b.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }

    protected override void OnModelCreating(ModelBuilder m) =>
        m.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = clock.Now;
        foreach (var e in ChangeTracker.Entries<Entity>())
        {
            if (e.State == EntityState.Added && e.Entity.CreatedAt == default) e.Entity.CreatedAt = now;
            if (e.State == EntityState.Modified) e.Entity.UpdatedAt = now;
            if (e.State == EntityState.Deleted) throw new InvalidOperationException("Hard delete is not allowed; use SoftDelete().");
        }
        foreach (var e in ChangeTracker.Entries())
        {
            // Append-only tables: never update or delete.
            if (e.Entity is GoldRateEntry or CashLedgerEntry or AuditLog && e.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException($"{e.Entity.GetType().Name} is append-only.");
        }
        return base.SaveChangesAsync(ct);
    }
}
