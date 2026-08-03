namespace SalesDeliveryBI.Infrastructure.Persistence.EfCore.Seed;

internal sealed class SeedQuotationRecord
{
    public required string QuotationNo { get; init; }
    public required string QuotationDate { get; init; }
    public required string BuyerName { get; init; }
    public required string MerchandiserName { get; init; }
    public required string UnitName { get; init; }
    public decimal Value { get; init; }
    public required string Status { get; init; }
    public int DaysOpen { get; init; }
    public string? ConvertedDate { get; init; }
    public string? LostReason { get; init; }
    public required string StyleNo { get; init; }
    public required string Season { get; init; }
}
