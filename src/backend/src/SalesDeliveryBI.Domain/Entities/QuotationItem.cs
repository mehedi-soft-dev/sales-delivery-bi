using SalesDeliveryBI.Domain.Common;

namespace SalesDeliveryBI.Domain.Entities;

public class QuotationItem : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation? Quotation { get; set; }

    public required string StyleNo { get; set; }
    public required string ItemDescription { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
