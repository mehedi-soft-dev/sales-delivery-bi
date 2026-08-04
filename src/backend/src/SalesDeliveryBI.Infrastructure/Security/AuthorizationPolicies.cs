using Microsoft.Extensions.DependencyInjection;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>
/// Permission-claim based, not role-name based (docs/plans/security/security-plan.md §3) — this API has no
/// idea what roles exist, only which permission codes the caller's token carries.
/// </summary>
public static class AuthorizationPolicies
{
    public const string QuotationRead = "QuotationRead";
    public const string QuotationReadAllUnits = "QuotationReadAllUnits";
    public const string AdminRead = "AdminRead";

    /// <summary>
    /// Claim type carrying permission codes in the JWT. security-plan.md's own §2 (claim contract) and §3
    /// (policy code sample) disagree on this name ("permissions" vs "permission") — using "permissions" here
    /// since that's what §2 says the Identity service actually puts in the token.
    /// </summary>
    private const string PermissionClaimType = "permissions";

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(QuotationRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationView));

            options.AddPolicy(QuotationReadAllUnits, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewAllUnits));

            options.AddPolicy(AdminRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.AdminView));
        });

        return services;
    }
}
