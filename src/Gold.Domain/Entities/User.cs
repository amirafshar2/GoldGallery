using Gold.Domain.Common;

namespace Gold.Domain.Entities;

public sealed class User : Entity
{
    public string Phone { get; private set; } = default!;
    public string FullName { get; private set; } = "";
    public UserRole Role { get; private set; }
    /// <summary>Demo accounts are the ONLY ones reachable through one-click demo login.</summary>
    public bool IsDemo { get; private set; }

    private User() { }

    public static User Create(string phone, UserRole role, string fullName = "", bool isDemo = false)
    {
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("Phone is required.");
        return new User { Phone = phone, Role = role, FullName = fullName, IsDemo = isDemo };
    }

    public void Rename(string fullName) => FullName = fullName.Trim();

    public void ChangeRole(UserRole role) => Role = role;
}

public sealed class OtpCode
{
    public long Id { get; private set; }
    public string Phone { get; private set; } = default!;
    public string CodeHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public int Attempts { get; private set; }
    public bool Used { get; private set; }

    private OtpCode() { }

    public static OtpCode Create(string phone, string codeHash, DateTimeOffset expiresAt) =>
        new() { Phone = phone, CodeHash = codeHash, ExpiresAt = expiresAt };

    public const int MaxAttempts = 5;

    public bool CanTry(DateTimeOffset now) => !Used && Attempts < MaxAttempts && now < ExpiresAt;
    public void RegisterAttempt() => Attempts++;
    public void MarkUsed() => Used = true;
}

public sealed class RefreshToken
{
    public long Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    /// <summary>All rotations of one login share a family; reuse of a revoked token revokes the family.</summary>
    public Guid FamilyId { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public bool IsDemo { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string hash, Guid familyId, DateTimeOffset expiresAt, bool isDemo) =>
        new() { UserId = userId, TokenHash = hash, FamilyId = familyId, ExpiresAt = expiresAt, IsDemo = isDemo };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
