using Microsoft.EntityFrameworkCore;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore;

/// <summary>Plain EF Core reads against the `sales` OLTP schema — same EF-vs-Dapper split as UserRepository.</summary>
public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken) =>
        await _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserUnits).ThenInclude(uu => uu.Unit)
            .OrderBy(u => u.DisplayName)
            .Select(u => new AdminUserDto(
                u.Id,
                u.Email,
                u.DisplayName,
                u.Role!.Name,
                u.IsActive,
                u.UserUnits.Select(uu => uu.Unit!.UnitName).ToList()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AdminRoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        List<Role> roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> userCountByRole = await _context.Users
            .GroupBy(u => u.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles
            .Select(r => new AdminRoleDto(
                r.Id,
                r.Name,
                userCountByRole.GetValueOrDefault(r.Id),
                r.RolePermissions.Select(rp => rp.PermissionCode).OrderBy(code => code).ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<AdminPermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        List<RolePermission> rolePermissions = await _context.RolePermissions
            .Include(rp => rp.Role)
            .ToListAsync(cancellationToken);

        return rolePermissions
            .GroupBy(rp => rp.PermissionCode)
            .Select(g => new AdminPermissionDto(
                g.Key,
                g.Select(rp => rp.Role!.Name).Distinct().OrderBy(name => name).ToList()))
            .OrderBy(p => p.PermissionCode)
            .ToList();
    }

    public async Task<AdminRoleDto?> SetRolePermissionsAsync(
        Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken)
    {
        Role? role = await _context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            return null;
        }

        HashSet<string> desired = permissionCodes.ToHashSet();
        List<RolePermission> toRemove = role.RolePermissions.Where(rp => !desired.Contains(rp.PermissionCode)).ToList();
        HashSet<string> current = role.RolePermissions.Select(rp => rp.PermissionCode).ToHashSet();

        _context.RolePermissions.RemoveRange(toRemove);
        foreach (string code in desired.Where(code => !current.Contains(code)))
        {
            _context.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionCode = code });
        }

        await _context.SaveChangesAsync(cancellationToken);

        int userCount = await _context.Users.CountAsync(u => u.RoleId == roleId, cancellationToken);

        return new AdminRoleDto(role.Id, role.Name, userCount, desired.OrderBy(code => code).ToList());
    }
}
