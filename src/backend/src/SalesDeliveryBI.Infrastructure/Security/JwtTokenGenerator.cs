using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>
/// Issues JWTs signed with the exact same key/algorithm Program.cs configures for validation
/// (Jwt:Issuer/Audience/SigningKey), so tokens minted here are guaranteed to validate against
/// this same process. Claim shape matches docs/plans/security/security-plan.md §2 exactly.
/// </summary>
public class JwtTokenGenerator : IJwtTokenGenerator
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAtUtc) Generate(User user, IReadOnlyCollection<Guid> unitIds)
    {
        string issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Issuer' configuration.");
        string audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Missing 'Jwt:Audience' configuration.");
        string signingKey = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Missing 'Jwt:SigningKey' configuration.");

        Role role = user.Role
            ?? throw new InvalidOperationException($"User '{user.Id}' has no loaded Role.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("name", user.DisplayName),
            // Display-only — never consulted for access control (that's `permissions`, checked below).
            new("role", role.Name),
        };

        claims.AddRange(role.RolePermissions.Select(rp => new Claim("permissions", rp.PermissionCode)));
        claims.AddRange(unitIds.Select(unitId => new Claim("user_units", unitId.ToString())));

        DateTime expiresAtUtc = DateTime.UtcNow.Add(TokenLifetime);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
