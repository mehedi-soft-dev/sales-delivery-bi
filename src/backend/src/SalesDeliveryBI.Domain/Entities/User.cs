using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class User : BaseEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid RoleId { get; set; }
    public Role? Role { get; set; }

    public ICollection<UserUnit> UserUnits { get; set; } = new List<UserUnit>();
}
