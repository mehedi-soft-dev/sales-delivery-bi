namespace SalesDeliveryBI.Application.Dtos;

public sealed record AgingDto(
    AgingKpisDto Kpis,
    IReadOnlyList<AgingBucketDto> AgingBuckets,
    IReadOnlyList<AgedQuotationDto> AgedQuotations);

public sealed record AgingKpisDto(decimal TotalOpenValueUsd, decimal HighRiskAgedValueUsd);

public sealed record AgingBucketDto(string Bucket, int Count, decimal ValueUsd);

public sealed record AgedQuotationDto(
    Guid QuotationId,
    string QuotationNo,
    string BuyerName,
    decimal ValueUsd,
    int DaysOpen,
    string Status,
    string RiskLevel);
