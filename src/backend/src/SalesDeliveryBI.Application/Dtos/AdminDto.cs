namespace SalesDeliveryBI.Application.Dtos;

/// <summary>
/// Admin > Users/Roles/Permissions DTOs (AuthorizationPolicies.AdminRead, SuperAdmin-only). Read-only except
/// role-permission mapping (AuthorizationPolicies.AdminWrite) — discussed-and-scoped-in as the one Admin write
/// path; user/role CRUD and unit assignment remain out of scope.
/// </summary>
public sealed record AdminUserDto(
    Guid UserId,
    string Email,
    string DisplayName,
    string RoleName,
    bool IsActive,
    IReadOnlyList<string> UnitNames);

public sealed record AdminRoleDto(
    Guid RoleId,
    string RoleName,
    int UserCount,
    IReadOnlyList<string> PermissionCodes);

public sealed record AdminPermissionDto(
    string PermissionCode,
    IReadOnlyList<string> RoleNames);

/// <summary>Body for PUT /api/admin/roles/{roleId}/permissions — the full desired permission-code set for the role.</summary>
public sealed record UpdateRolePermissionsRequestDto(IReadOnlyList<string> PermissionCodes);
