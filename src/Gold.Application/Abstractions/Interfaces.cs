using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<OtpCode> OtpCodes { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Product> Products { get; }
    DbSet<Piece> Pieces { get; }
    DbSet<GoldRateEntry> GoldRates { get; }
    DbSet<PricingSettings> PricingSettings { get; }
    DbSet<PriceQuote> PriceQuotes { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CashLedgerEntry> CashLedger { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> Outbox { get; }
    DbSet<SiteSetting> SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IClock
{
    DateTimeOffset Now { get; }
}

public interface ICurrentUser
{
    Guid? Id { get; }
    UserRole? Role { get; }
    bool IsDemo { get; }
}

public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct);
}

public interface ISecretHasher
{
    string Hash(string value);
    bool Verify(string value, string hash);
}

public sealed record IssuedTokens(string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, bool isDemo);
    string CreateRefreshTokenValue();
}

/// <summary>Raised after a rate change so connected clients (SignalR) and caches update.</summary>
public interface IRateNotifier
{
    Task RateChangedAsync(Rates.RateDto rate, CancellationToken ct);
}

public sealed record GatewayStart(string Token, string RedirectUrl);
public sealed record GatewayVerify(bool Success, long AmountRial, string TrackingCode);

/// <summary>Payment gateway port. Real adapters: Parbad (Mellat, Saman, ZarinPal ...). Dev: FakeGateway.</summary>
public interface IPaymentGateway
{
    string Name { get; }
    Task<GatewayStart> StartAsync(Guid paymentId, long amountRial, string callbackUrl, CancellationToken ct);
    Task<GatewayVerify> VerifyAsync(string token, CancellationToken ct);
}
