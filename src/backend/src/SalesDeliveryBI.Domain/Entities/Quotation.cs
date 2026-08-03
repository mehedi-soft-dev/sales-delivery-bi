using SalesDeliveryBI.Domain.Common;
using SalesDeliveryBI.Domain.Enums;

namespace SalesDeliveryBI.Domain.Entities;

public class Quotation : BaseEntity
{
    public required string QuotationNo { get; set; }
    public DateOnly QuotationDate { get; set; }

    public Guid BuyerId { get; set; }
    public Buyer? Buyer { get; set; }

    public Guid MerchandiserId { get; set; }
    public Merchandiser? Merchandiser { get; set; }

    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }

    public required string StyleNo { get; set; }
    public required string Season { get; set; }
    public required string CurrencyCode { get; set; }

    // Net total after Discount, in CurrencyCode. Subtotal (pre-discount) = Value + Discount, computed at read time — not stored.
    public decimal Value { get; set; }

    public required string Incoterm { get; set; }
    public required string PaymentTerm { get; set; }
    public DateOnly ValidUntil { get; set; }
    public decimal Discount { get; set; }

    public QuotationStatus Status { get; set; }
    public DateTime StatusDate { get; set; }

    public string? ConvertedToSoNo { get; set; }
    public DateTime? ConvertedDate { get; set; }
    public string? LostReason { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    public ICollection<QuotationStatusHistory> StatusHistory { get; set; } = new List<QuotationStatusHistory>();
}
