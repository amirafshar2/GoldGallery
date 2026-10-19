using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Gold.Application.Abstractions;
using Gold.Application.Common;
using Gold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gold.Application.Auth;

public sealed record RequestOtpRequest(string Phone);
public sealed record VerifyOtpRequest(string Phone, string Code);
public sealed record DemoLoginRequest(string Role);
public sealed record MeDto(Guid Id, string Phone, string FullName, string Role, bool IsDemo);
public sealed record AuthResult(MeDto User, string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public sealed partial class AuthService(IAppDbContext db, IClock clock, ISmsSender sms, ISecretHasher hasher, ITokenService tokens)
{
    public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(14);

    /// <summary>Only these roles can be used for one-click demo login (whitelist, never free input).</summary>
    public static readonly IReadOnlyDictionary<string, UserRole> DemoRoles = new Dictionary<string, UserRole>(StringComparer.OrdinalIgnoreCase)
    {
        ["customer"] = UserRole.Customer,
        ["wholesale"] = UserRole.Wholesale,
        ["staff"] = UserRole.Staff,
        ["admin"] = UserRole.Admin,
    };

    [GeneratedRegex(@"^09\d{9}$")]
    public static partial Regex IranMobile();

    public static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Trim().Select(ch => ch switch
        {
            >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
            >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
            _ => ch,
        }).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
        if (digits.Length == 10 && digits.StartsWith('9')) digits = "0" + digits;
        return digits;
    }

    public async Task<Result<Unit>> RequestOtpAsync(RequestOtpRequest req, CancellationToken ct)
    {
        var phone = NormalizePhone(req.Phone);
        if (!IranMobile().IsMatch(phone)) return Error.Validation("phone.invalid", "شماره موبایل معتبر نیست.");

        var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        db.OtpCodes.Add(OtpCode.Create(phone, hasher.Hash(code), clock.Now.Add(OtpLifetime)));
        await db.SaveChangesAsync(ct);
        await sms.SendAsync(phone, $"کد ورود شما: {code}", ct);
        return Unit.Value;
    }

    public async Task<Result<AuthResult>> VerifyOtpAsync(VerifyOtpRequest req, CancellationToken ct)
    {
        var phone = NormalizePhone(req.Phone);
        var otp = await db.OtpCodes.Where(o => o.Phone == phone).OrderByDescending(o => o.Id).FirstOrDefaultAsync(ct);
        if (otp is null || !otp.CanTry(clock.Now)) return Error.Unauthorized("otp.invalid", "کد منقضی یا نامعتبر است.");

        otp.RegisterAttempt();
        if (!hasher.Verify(req.Code.Trim(), otp.CodeHash))
        {
            await db.SaveChangesAsync(ct);
            return Error.Unauthorized("otp.invalid", "کد وارد شده اشتباه است.");
        }
        otp.MarkUsed();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);
        if (user is null)
        {
            user = User.Create(phone, UserRole.Customer);
            db.Users.Add(user);
        }
        var result = Issue(user, isDemo: false, familyId: Guid.NewGuid());
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<Result<AuthResult>> DemoLoginAsync(DemoLoginRequest req, CancellationToken ct)
    {
        if (!DemoRoles.TryGetValue(req.Role ?? "", out var role)) return Error.Validation("demo.role", "نقش نامعتبر است.");
        var user = await db.Users.FirstOrDefaultAsync(u => u.IsDemo && u.Role == role, ct);
        if (user is null) return Error.NotFound("demo.user", "حساب نمایشی این نقش وجود ندارد.");

        var result = Issue(user, isDemo: true, familyId: Guid.NewGuid());
        db.AuditLogs.Add(AuditLog.Create(clock.Now, user.Id, "auth.demoLogin", "User", user.Id.ToString(), role.ToString(), isDemo: true));
        await db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>Roles that really have a demo account (an empty install has none → no demo buttons).</summary>
    public async Task<IReadOnlyList<string>> AvailableDemoRolesAsync(CancellationToken ct)
    {
        var roles = await db.Users.AsNoTracking().Where(u => u.IsDemo).Select(u => u.Role).Distinct().ToListAsync(ct);
        return DemoRoles.Where(kv => roles.Contains(kv.Value)).Select(kv => kv.Key).ToList();
    }

    public async Task<Result<AuthResult>> RefreshAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return Error.Unauthorized("refresh.missing", "نشست منقضی شده است.");
        var hash = hasher.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return Error.Unauthorized("refresh.invalid", "نشست نامعتبر است.");

        if (!stored.IsActive(clock.Now))
        {
            if (stored.RevokedAt is not null)
            {
                // Reuse of a rotated token → assume theft, revoke the whole family.
                var family = await db.RefreshTokens.Where(t => t.FamilyId == stored.FamilyId).ToListAsync(ct);
                family.ForEach(t => t.Revoke(clock.Now));
                db.AuditLogs.Add(AuditLog.Create(clock.Now, stored.UserId, "auth.refreshReuse", "RefreshToken", stored.FamilyId.ToString()));
                await db.SaveChangesAsync(ct);
            }
            return Error.Unauthorized("refresh.invalid", "نشست نامعتبر است.");
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
        if (user is null) return Error.Unauthorized("refresh.invalid", "نشست نامعتبر است.");

        stored.Revoke(clock.Now);
        var result = Issue(user, stored.IsDemo, stored.FamilyId);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = hasher.Hash(refreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return;
        var family = await db.RefreshTokens.Where(t => t.FamilyId == stored.FamilyId).ToListAsync(ct);
        family.ForEach(t => t.Revoke(clock.Now));
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<MeDto>> MeAsync(Guid userId, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, ct);
        return u is null ? Error.NotFound("user.notFound", "کاربر پیدا نشد.") : ToMe(u);
    }

    private AuthResult Issue(User user, bool isDemo, Guid familyId)
    {
        var (access, accessExp) = tokens.CreateAccessToken(user, isDemo);
        var refresh = tokens.CreateRefreshTokenValue();
        var refreshExp = clock.Now.Add(RefreshLifetime);
        db.RefreshTokens.Add(RefreshToken.Create(user.Id, hasher.Hash(refresh), familyId, refreshExp, isDemo));
        return new AuthResult(ToMe(user), access, accessExp, refresh, refreshExp);
    }

    private static MeDto ToMe(User u) => new(u.Id, u.Phone, u.FullName, u.Role.ToString(), u.IsDemo);
}
