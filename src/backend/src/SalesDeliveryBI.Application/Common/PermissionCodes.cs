namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Permission codes per docs/plans/security/security-plan.md §1 — {module}.{resource}.{action}. Lives in
/// Application (not Infrastructure/Security, despite backing AuthorizationPolicies there) because
/// AdminAppService.UpdateRolePermissionsAsync needs the canonical allow-list too, and Application can't
/// depend on Infrastructure.
/// </summary>
public static class PermissionCodes
{
    public const string QuotationView = "bi.quotation.view";
    public const string QuotationViewAllUnits = "bi.quotation.viewAllUnits";

    /// <summary>View-only access to Admin > Users/Roles/Permissions — seeded to SuperAdmin only (DatabaseSeeder).</summary>
    public const string AdminView = "admin.access.view";

    /// <summary>
    /// Write access to Admin — currently just role-permission mapping (AdminController.UpdateRolePermissions),
    /// discussed-and-scoped-in as the one Admin write path; user/role CRUD and unit assignment remain out of
    /// scope. Seeded to SuperAdmin only (DatabaseSeeder), same as AdminView.
    /// </summary>
    public const string AdminManage = "admin.access.manage";

    /// <summary>Every permission code the system knows about — the allow-list UpdateRolePermissionsAsync validates against.</summary>
    public static readonly string[] All = [QuotationView, QuotationViewAllUnits, AdminView, AdminManage];
}
