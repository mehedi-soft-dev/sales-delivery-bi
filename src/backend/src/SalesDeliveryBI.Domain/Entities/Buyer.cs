using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class Buyer : BaseEntity
{
    public required string BuyerName { get; set; }
}
