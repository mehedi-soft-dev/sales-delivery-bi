using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>
/// Row-level unit security, per docs/plans/security/security-plan.md §4. Every QuotationAppService
/// method calls this explicitly before touching the repository — there is no pipeline enforcing it.
/// </summary>
public interface IUnitAccessGuard
{
    /// <exception cref="ForbiddenAccessException">
    /// Caller lacks bi.quotation.viewAllUnits and requestedUnitId is outside their assigned units.
    /// </exception>
    UnitScope Validate(Guid? requestedUnitId);
}
