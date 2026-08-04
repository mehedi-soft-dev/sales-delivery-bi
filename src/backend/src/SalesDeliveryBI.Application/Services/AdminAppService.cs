using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>
/// Plain read-only AppService for Admin > Users/Roles/Permissions (AuthorizationPolicies.AdminRead,
/// SuperAdmin-only) — no create/edit/delete counterparts exist, by design (docs/plans/security/security-plan.md).
/// Uncached: `sales.Users`/`Roles`/`RolePermissions` are live OLTP rows with no scheduled refresh job, unlike
/// the bi.* dashboards, so there's no `lastRefresh` concept here and no ICacheService/IUnitAccessGuard involved.
/// </summary>
public class AdminAppService
{
    private static readonly IReadOnlyDictionary<string, Func<AdminUserDto, IComparable>> UserSortSelectors =
        new Dictionary<string, Func<AdminUserDto, IComparable>>
        {
            ["email"] = u => u.Email,
            ["displayName"] = u => u.DisplayName,
            ["roleName"] = u => u.RoleName,
            ["isActive"] = u => u.IsActive,
        };

    private static readonly IReadOnlyDictionary<string, Func<AdminRoleDto, IComparable>> RoleSortSelectors =
        new Dictionary<string, Func<AdminRoleDto, IComparable>>
        {
            ["roleName"] = r => r.RoleName,
            ["userCount"] = r => r.UserCount,
        };

    private static readonly IReadOnlyDictionary<string, Func<AdminPermissionDto, IComparable>> PermissionSortSelectors =
        new Dictionary<string, Func<AdminPermissionDto, IComparable>>
        {
            ["permissionCode"] = p => p.PermissionCode,
        };

    private readonly IAdminRepository _repository;

    public AdminAppService(IAdminRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(GridQuery grid, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminUserDto> users = await _repository.GetUsersAsync(cancellationToken);
        return GridPaging.Apply(users, grid, UserSortSelectors);
    }

    public async Task<PagedResult<AdminRoleDto>> GetRolesAsync(GridQuery grid, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminRoleDto> roles = await _repository.GetRolesAsync(cancellationToken);
        return GridPaging.Apply(roles, grid, RoleSortSelectors);
    }

    public async Task<PagedResult<AdminPermissionDto>> GetPermissionsAsync(GridQuery grid, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminPermissionDto> permissions = await _repository.GetPermissionsAsync(cancellationToken);
        return GridPaging.Apply(permissions, grid, PermissionSortSelectors);
    }

    /// <exception cref="ArgumentException">A permission code isn't in PermissionCodes.All.</exception>
    public async Task<AdminRoleDto?> UpdateRolePermissionsAsync(
        Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        string[] unknown = permissionCodes.Except(PermissionCodes.All).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown permission code(s): {string.Join(", ", unknown)}");
        }

        return await _repository.SetRolePermissionsAsync(roleId, permissionCodes, cancellationToken);
    }
}
