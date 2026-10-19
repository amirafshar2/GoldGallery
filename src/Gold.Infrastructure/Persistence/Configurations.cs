using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gold.Infrastructure.Persistence;

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Phone).HasMaxLength(11).IsRequired();
        b.HasIndex(x => x.Phone).IsUnique();
        b.Property(x => x.FullName).HasMaxLength(120);
        b.HasIndex(x => new { x.IsDemo, x.Role });
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class OtpConfig : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Phone).HasMaxLength(11);
        b.Property(x => x.CodeHash).HasMaxLength(100);
        b.HasIndex(x => x.Phone);
    }
}

internal sealed class RefreshConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).HasMaxLength(100);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.FamilyId);
    }
}

internal sealed class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).HasMaxLength(80).IsRequired();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class BranchConfig : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Address).HasMaxLength(300);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class ProductConfig : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.Audience, x.Color });
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Pieces).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.Navigation(x => x.Pieces).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Ignore(x => x.Wage);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class PieceConfig : IEntityTypeConfiguration<Piece>
{
    public void Configure(EntityTypeBuilder<Piece> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Barcode).HasMaxLength(40).IsRequired();
        b.Property(x => x.Size).HasMaxLength(20);
        b.HasIndex(x => x.Barcode).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.Status });
        b.HasIndex(x => x.GoldWeightMg);
        b.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Ignore(x => x.GoldWeight); b.Ignore(x => x.Karat); b.Ignore(x => x.StoneValue);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class RateConfig : IEntityTypeConfiguration<GoldRateEntry>
{
    public void Configure(EntityTypeBuilder<GoldRateEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Source).HasMaxLength(40);
    }
}

internal sealed class PricingSettingsConfig : IEntityTypeConfiguration<PricingSettings>
{
    public void Configure(EntityTypeBuilder<PricingSettings> b) => b.HasKey(x => x.Id);
}

internal sealed class QuoteConfig : IEntityTypeConfiguration<PriceQuote>
{
    public void Configure(EntityTypeBuilder<PriceQuote> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.PieceIdsCsv).HasMaxLength(400);
        b.Ignore(x => x.PieceIds);
        b.HasIndex(x => x.UserId);
    }
}

internal sealed class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Number).HasMaxLength(30);
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.UserId, x.Status });
        b.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class OrderItemConfig : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(x => x.Id);
        // DB-level guarantee: one ACTIVE order per piece.
        b.HasIndex(x => x.PieceId).IsUnique().HasFilter("IsActive = 1").HasDatabaseName("UX_OrderItems_ActivePiece");
    }
}

internal sealed class PaymentConfig : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.IdempotencyKey).IsUnique();
        b.HasIndex(x => x.OrderId);
        b.Property(x => x.Gateway).HasMaxLength(40);
        b.Property(x => x.GatewayToken).HasMaxLength(200);
        b.Property(x => x.TrackingCode).HasMaxLength(100);
        b.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}

internal sealed class LedgerConfig : IEntityTypeConfiguration<CashLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CashLedgerEntry> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(60);
        b.HasIndex(x => x.PaymentId);
    }
}

internal sealed class AuditConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(60);
        b.Property(x => x.Entity).HasMaxLength(60);
        b.Property(x => x.EntityId).HasMaxLength(60);
        b.Property(x => x.Details).HasMaxLength(1000);
    }
}

internal sealed class OutboxConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(60);
        b.HasIndex(x => x.ProcessedAt);
    }
}

internal sealed class SiteSettingConfig : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> b)
    {
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(40);
    }
}
