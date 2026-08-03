namespace SalesDeliveryBI.Application.Dtos;

public sealed record QuotationDetailDto(
    Guid QuotationId,
    string QuotationNo,
    DateOnly QuotationDate,
    string BuyerName,
    string MerchandiserName,
    string UnitName,
    string StyleNo,
    string Season,
    string CurrencyCode,
    decimal QuotationValueUsd,
    string Incoterm,
    string PaymentTerm,
    DateOnly ValidUntil,
    decimal DiscountUsd,
    decimal SubtotalUsd,
    string Status,
    DateTime StatusDate,
    int DaysInStatus,
    int DaysOpen,
    string? ConvertedToSoNo,
    DateTime? ConvertedDate,
    int? ConversionDays,
    string? LostReason,
    Guid CreatedBy,
    IReadOnlyList<QuotationItemDto> Items,
    IReadOnlyList<QuotationStatusHistoryDto> StatusHistory);

/// <summary>UnitPrice/Amount are in the quotation's CurrencyCode — unlike the header total, line items are not FX-converted.</summary>
public sealed record QuotationItemDto(
    string StyleNo,
    string ItemDescription,
    int Qty,
    decimal UnitPrice,
    decimal Amount);

public sealed record QuotationStatusHistoryDto(string Status, DateTime StatusDate);
