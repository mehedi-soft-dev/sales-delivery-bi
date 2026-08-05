using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>
/// Row-level unit security, per docs/plans/security/security-plan.md §4. Every module's AppService calls
/// this explicitly before touching its repository — there is no pipeline enforcing it.
/// </summary>
public interface IUnitAccessGuard
{
    /// <summary>Quotation-module shorthand — equivalent to <c>Validate(requestedUnitId, PermissionCodes.QuotationViewAllUnits)</c>.</summary>
    /// <exception cref="ForbiddenAccessException">
    /// Caller lacks bi.quotation.viewAllUnits and requestedUnitId is outside their assigned units.
    /// </exception>
    UnitScope Validate(Guid? requestedUnitId);

    /// <summary>
    /// Generalized form for other modules (Sales Order/Delivery/Invoice/Return), each with their own
    /// all-units permission code — otherwise identical semantics to <see cref="Validate(Guid?)"/>.
    /// </summary>
    /// <exception cref="ForbiddenAccessException">
    /// Caller lacks <paramref name="allUnitsPermissionCode"/> and requestedUnitId is outside their assigned units.
    /// </exception>
    UnitScope Validate(Guid? requestedUnitId, string allUnitsPermissionCode);
}
