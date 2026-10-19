using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gold.Application.Abstractions;
using Gold.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Gold.Api.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "gold-api";
    public string Audience { get; set; } = "gold-web";
    public string Key { get; set; } = "";
    public int AccessMinutes { get; set; } = 15;
}

public sealed class JwtTokenService(JwtOptions opt, IClock clock) : ITokenService
{
    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, bool isDemo)
    {
        var exp = clock.Now.AddMinutes(opt.AccessMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("demo", isDemo ? "1" : "0"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.Key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(opt.Issuer, opt.Audience, claims, clock.Now.UtcDateTime, exp.UtcDateTime, creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), exp);
    }

    public string CreateRefreshTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}

public sealed class HttpCurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    private ClaimsPrincipal? P => http.HttpContext?.User;

    public Guid? Id => Guid.TryParse(P?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? P?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    public UserRole? Role => Enum.TryParse<UserRole>(P?.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;
    public bool IsDemo => P?.FindFirstValue("demo") == "1";
}

public static class Policies
{
    public const string Admin = "admin";
    public const string Staff = "staff";       // Staff or Admin
    public const string Customer = "customer"; // any signed-in user
    /// <summary>Real admin only — demo admin cannot touch gateway settings, roles or hard data.</summary>
    public const string RealAdmin = "realAdmin";
}
