namespace SalesDeliveryBI.Infrastructure.Security;

/// <summary>Permission codes per docs/plans/security/security-plan.md §1 — {module}.{resource}.{action}.</summary>
public static class PermissionCodes
{
    public const string QuotationView = "bi.quotation.view";
    public const string QuotationViewAllUnits = "bi.quotation.viewAllUnits";
}
