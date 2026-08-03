using SalesDeliveryBI.Domain.Common;
using SalesDeliveryBI.Domain.Enums;

namespace SalesDeliveryBI.Domain.Entities;

public class QuotationStatusHistory : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public QuotationStatus Status { get; set; }
    public DateTime StatusDate { get; set; }
    public string? Note { get; set; }
}
