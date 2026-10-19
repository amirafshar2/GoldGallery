using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Gold.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Gold.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}

/// <summary>Deterministic HMAC-SHA256 (needed for refresh-token lookup) with constant-time verify.</summary>
public sealed class HmacSecretHasher(string key) : ISecretHasher
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(key);

    public string Hash(string value) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));

    public bool Verify(string value, string hash) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(value)), Encoding.ASCII.GetBytes(hash));
}

/// <summary>Development SMS sender: writes the message to the log instead of sending it.</summary>
public sealed class LogSmsSender(ILogger<LogSmsSender> log) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct)
    {
        log.LogWarning("DEV SMS to {Phone}: {Message}", phone[..4] + "***" + phone[^2..], message);
        LastMessages[phone] = message;
        return Task.CompletedTask;
    }

    /// <summary>Exposed for tests/dev tools only.</summary>
    public static readonly ConcurrentDictionary<string, string> LastMessages = new();
}

/// <summary>
/// Sandbox gateway for local development and tests. The redirect page (/dev/fake-gateway) lets you
/// pay, cancel, or tamper with the amount to exercise every branch of the callback.
/// Replace with a Parbad adapter in production.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public sealed record FakeTx(Guid PaymentId, long Amount, string CallbackUrl)
    {
        public string? Outcome { get; set; } // "paid" | "cancelled" | "tampered"
    }

    public static readonly ConcurrentDictionary<string, FakeTx> Transactions = new();

    public string Name => "fake";

    public Task<GatewayStart> StartAsync(Guid paymentId, long amountRial, string callbackUrl, CancellationToken ct)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
        Transactions[token] = new FakeTx(paymentId, amountRial, callbackUrl);
        return Task.FromResult(new GatewayStart(token, $"/dev/fake-gateway?token={token}"));
    }

    public Task<GatewayVerify> VerifyAsync(string token, CancellationToken ct)
    {
        if (!Transactions.TryGetValue(token, out var tx)) return Task.FromResult(new GatewayVerify(false, 0, ""));
        return Task.FromResult(tx.Outcome switch
        {
            "paid" => new GatewayVerify(true, tx.Amount, "TRK" + token[..8]),
            "tampered" => new GatewayVerify(true, tx.Amount - 10_000, "TRK" + token[..8]),
            _ => new GatewayVerify(false, 0, ""),
        });
    }
}
