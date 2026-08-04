namespace SalesDeliveryBI.Domain.Entities;

/// <summary>
/// Well-known `Role.Name` values seeded into `sales.Roles` (docs/requirements/Sales_Delivery_Module_BI_Developer_Guidelines.md §5).
/// Roles themselves are real seeded table rows, not an enum — these constants exist only so
/// code that must reference a specific role by name (seeding, permission mapping) doesn't do it
/// via magic strings.
/// </summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string GeneralManager = "GeneralManager";
    public const string CommercialManager = "CommercialManager";
    public const string CommercialOfficer = "CommercialOfficer";
    public const string Merchandiser = "Merchandiser";
    public const string FinanceManager = "FinanceManager";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        SuperAdmin, GeneralManager, CommercialManager, CommercialOfficer, Merchandiser, FinanceManager, Viewer,
    ];
}
