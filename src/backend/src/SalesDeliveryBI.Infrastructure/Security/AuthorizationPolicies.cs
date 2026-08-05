using Microsoft.Extensions.DependencyInjection;
using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>
/// Permission-claim based, not role-name based (docs/plans/security/security-plan.md §3) — this API has no
/// idea what roles exist, only which permission codes the caller's token carries.
/// </summary>
public static class AuthorizationPolicies
{
    public const string QuotationPipelineRead = "QuotationPipelineRead";
    public const string QuotationConversionRead = "QuotationConversionRead";
    public const string QuotationAgingRead = "QuotationAgingRead";
    public const string QuotationSummaryRead = "QuotationSummaryRead";

    /// <summary>
    /// Satisfied by any one of the four per-dashboard view permissions — gates GetUnits/GetById, which aren't
    /// tied to a single dashboard (the unit dropdown and quotation detail drill-down are used from all of them).
    /// </summary>
    public const string QuotationViewAny = "QuotationViewAny";

    public const string QuotationReadAllUnits = "QuotationReadAllUnits";
    public const string AdminRead = "AdminRead";
    public const string AdminWrite = "AdminWrite";

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
            options.AddPolicy(QuotationPipelineRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewPipeline));

            options.AddPolicy(QuotationConversionRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewConversion));

            options.AddPolicy(QuotationAgingRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewAging));

            options.AddPolicy(QuotationSummaryRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewSummary));

            options.AddPolicy(QuotationViewAny, policy =>
                policy.RequireClaim(
                    PermissionClaimType,
                    PermissionCodes.QuotationViewPipeline,
                    PermissionCodes.QuotationViewConversion,
                    PermissionCodes.QuotationViewAging,
                    PermissionCodes.QuotationViewSummary));

            options.AddPolicy(QuotationReadAllUnits, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.QuotationViewAllUnits));

            options.AddPolicy(AdminRead, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.AdminView));

            options.AddPolicy(AdminWrite, policy =>
                policy.RequireClaim(PermissionClaimType, PermissionCodes.AdminManage));
        });

        return services;
    }
}
