using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SalesDeliveryBI.Application.Abstractions;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>
/// Reads sub/permissions/user_units from the current request's JWT claims (docs/plans/security/security-plan.md §2).
/// Outside an HTTP request — startup seeding, Quartz jobs (Phase 8) — there is no caller to read, so it
/// falls back to the fixed system identity rather than throwing.
/// </summary>
public class CurrentUserContext : ICurrentUserContext
{
    private const string SubClaimType = "sub";
    private const string PermissionsClaimType = "permissions";
    private const string UnitIdsClaimType = "user_units";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            string? sub = User?.FindFirst(SubClaimType)?.Value;
            return sub is null ? SystemCurrentUserContext.SystemUserId : Guid.Parse(sub);
        }
    }

    public IReadOnlyCollection<string> Permissions =>
        User?.FindAll(PermissionsClaimType).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public IReadOnlyCollection<Guid> UnitIds =>
        User?.FindAll(UnitIdsClaimType).Select(c => Guid.Parse(c.Value)).ToArray() ?? Array.Empty<Guid>();
}
