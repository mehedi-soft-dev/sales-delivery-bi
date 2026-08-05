namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Permission codes per docs/plans/security/security-plan.md §1 — {module}.{resource}.{action}. Lives in
/// Application (not Infrastructure/Security, despite backing AuthorizationPolicies there) because
/// AdminAppService.UpdateRolePermissionsAsync needs the canonical allow-list too, and Application can't
/// depend on Infrastructure.
/// </summary>
public static class PermissionCodes
{
    /// <summary>
    /// Per-dashboard view permissions — split from the original single bi.quotation.view (discussed with the
    /// user) because docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md §5 gives different
    /// roles access to different dashboards (e.g. FinanceManager = conversion/value only, Viewer = summary
    /// only) and one blanket code couldn't express that.
    /// </summary>
    public const string QuotationViewPipeline = "bi.quotation.view.pipeline";
    public const string QuotationViewConversion = "bi.quotation.view.conversion";
    public const string QuotationViewAging = "bi.quotation.view.aging";
    public const string QuotationViewSummary = "bi.quotation.view.summary";

    public const string QuotationViewAllUnits = "bi.quotation.viewAllUnits";

    /// <summary>
    /// Sales Order module — one code for the one dashboard (unlike Quotations' 3-way split, this module has
    /// a single dashboard per docs/requirements/Sales_Delivery_BI_Implementation_Proposal.md's later extension).
    /// </summary>
    public const string SalesOrderView = "bi.salesorder.view";
    public const string SalesOrderViewAllUnits = "bi.salesorder.viewAllUnits";

    /// <summary>Delivery/Challan module — one dashboard, same single-code shape as Sales Order.</summary>
    public const string DeliveryView = "bi.delivery.view";
    public const string DeliveryViewAllUnits = "bi.delivery.viewAllUnits";

    /// <summary>Sales Invoice module — one dashboard, same single-code shape as Sales Order.</summary>
    public const string InvoiceView = "bi.invoice.view";
    public const string InvoiceViewAllUnits = "bi.invoice.viewAllUnits";

    /// <summary>Return/Credit Note module — one dashboard, same single-code shape as Sales Order.</summary>
    public const string ReturnView = "bi.return.view";
    public const string ReturnViewAllUnits = "bi.return.viewAllUnits";

    /// <summary>View-only access to Admin > Users/Roles/Permissions — seeded to SuperAdmin only (DatabaseSeeder).</summary>
    public const string AdminView = "admin.access.view";

    /// <summary>
    /// Write access to Admin — currently just role-permission mapping (AdminController.UpdateRolePermissions),
    /// discussed-and-scoped-in as the one Admin write path; user/role CRUD and unit assignment remain out of
    /// scope. Seeded to SuperAdmin only (DatabaseSeeder), same as AdminView.
    /// </summary>
    public const string AdminManage = "admin.access.manage";

    /// <summary>Every permission code the system knows about — the allow-list UpdateRolePermissionsAsync validates against.</summary>
    public static readonly string[] All =
    [
        QuotationViewPipeline, QuotationViewConversion, QuotationViewAging, QuotationViewSummary,
        QuotationViewAllUnits, SalesOrderView, SalesOrderViewAllUnits,
        DeliveryView, DeliveryViewAllUnits, InvoiceView, InvoiceViewAllUnits, ReturnView, ReturnViewAllUnits,
        AdminView, AdminManage,
    ];
}
