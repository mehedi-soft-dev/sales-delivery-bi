namespace SalesDeliveryBI.Application.Dtos;

/// <summary>View-only Admin > Users/Roles/Permissions DTOs (AuthorizationPolicies.AdminRead, SuperAdmin-only) — no create/edit/delete counterparts exist.</summary>
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
