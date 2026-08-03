using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class Unit : BaseEntity
{
    public required string UnitName { get; set; }
    public required string UnitType { get; set; }
}
