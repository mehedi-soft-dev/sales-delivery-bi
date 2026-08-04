using SalesDeliveryBI.Domain.Entities;

namespace SalesDeliveryBI.Application.Abstractions;

public interface IUserRepository
{
    /// <summary>Includes Role (with its RolePermissions) and UserUnits — everything a login needs in one round trip.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
