using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>
/// Plain EF Core reads over the `sales` OLTP schema (Users/Roles/RolePermissions/UserUnits) — not `bi.*`,
/// so no IUnitAccessGuard/ICacheService involved (those are specific to the cached BI dashboards).
/// Each method returns the FULL unpaged list; AdminAppService applies GridPaging, same split as
/// QuotationAppService/IQuotationRepository.
/// </summary>
public interface IAdminRepository
{
    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminPermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken);

    /// <summary>Replaces the role's full permission-code set. Returns null if no role with <paramref name="roleId"/> exists.</summary>
    Task<AdminRoleDto?> SetRolePermissionsAsync(
        Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken);
}
