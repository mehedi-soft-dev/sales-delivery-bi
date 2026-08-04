using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class UserUnit : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
}
