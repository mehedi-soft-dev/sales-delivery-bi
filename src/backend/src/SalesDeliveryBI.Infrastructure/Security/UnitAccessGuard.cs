using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>Row-level unit security — docs/plans/security/security-plan.md §4, followed exactly.</summary>
public class UnitAccessGuard : IUnitAccessGuard
{
    private readonly ICurrentUserContext _currentUserContext;

    public UnitAccessGuard(ICurrentUserContext currentUserContext)
    {
        _currentUserContext = currentUserContext;
    }

    public UnitScope Validate(Guid? requestedUnitId) => Validate(requestedUnitId, PermissionCodes.QuotationViewAllUnits);

    public UnitScope Validate(Guid? requestedUnitId, string allUnitsPermissionCode)
    {
        bool hasViewAllUnits = _currentUserContext.Permissions.Contains(allUnitsPermissionCode);

        if (hasViewAllUnits)
        {
            return requestedUnitId is { } unitId ? UnitScope.RestrictedTo([unitId]) : UnitScope.Unrestricted();
        }

        IReadOnlyCollection<Guid> assignedUnits = _currentUserContext.UnitIds;

        if (requestedUnitId is null)
        {
            return UnitScope.RestrictedTo(assignedUnits);
        }

        if (!assignedUnits.Contains(requestedUnitId.Value))
        {
            throw new ForbiddenAccessException($"Unit '{requestedUnitId}' is outside the caller's assigned units.");
        }

        return UnitScope.RestrictedTo([requestedUnitId.Value]);
    }
}
