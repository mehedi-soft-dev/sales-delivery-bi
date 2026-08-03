using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class FxRate : BaseEntity
{
    public required string CurrencyCode { get; set; }
    public DateOnly RateDate { get; set; }
    public decimal RateToUsd { get; set; }
}
