using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SalesDeliveryBI.Api.Tests;

/// <summary>Mints test JWTs matching security-plan.md §2's claim shape, signed with the dev-only key from appsettings.Development.json.</summary>
internal static class TestJwtTokenFactory
{
    private const string Issuer = "https://localhost/identity";
    private const string Audience = "salesdeliverybi";
    private const string SigningKey = "dev-only-signing-key-replace-once-the-identity-service-exists-0123456789";

    public static string Create(Guid userId, IEnumerable<string> permissions, IEnumerable<Guid> unitIds)
    {
        var claims = new List<Claim> { new("sub", userId.ToString()) };
        claims.AddRange(permissions.Select(p => new Claim("permissions", p)));
        claims.AddRange(unitIds.Select(u => new Claim("user_units", u.ToString())));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            Issuer,
            Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
