using Microsoft.EntityFrameworkCore;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore;

/// <summary>
/// Plain EF Core query against the `sales` OLTP schema — Users isn't a `bi.*` materialized view,
/// so this stays EF Core rather than Dapper (docs/plans/backend/architecture.md's EF-vs-Dapper split).
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await _context.Users
            .Include(u => u.Role!).ThenInclude(r => r.RolePermissions)
            .Include(u => u.UserUnits)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
}
