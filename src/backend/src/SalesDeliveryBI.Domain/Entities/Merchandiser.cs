using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class Merchandiser : BaseEntity
{
    public required string MerchandiserName { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
}
