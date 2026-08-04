using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — AgedQuotations is the FULL unpaged list, exactly what's cached under CacheKeys.Aging.</summary>
public sealed record AgingDto(
    AgingKpisDto Kpis,
    IReadOnlyList<AgingBucketDto> AgingBuckets,
    IReadOnlyList<RiskLevelBucketDto> RiskLevels,
    IReadOnlyList<AgedQuotationDto> AgedQuotations);

/// <summary>What the API actually returns — AgedQuotations is the server-side-paged slice, sliced from the cached full list in QuotationAppService.</summary>
public sealed record AgingResponseDto(
    AgingKpisDto Kpis,
    IReadOnlyList<AgingBucketDto> AgingBuckets,
    IReadOnlyList<RiskLevelBucketDto> RiskLevels,
    PagedResult<AgedQuotationDto> AgedQuotations);

public sealed record AgingKpisDto(decimal TotalOpenValueUsd, decimal HighRiskAgedValueUsd);

public sealed record AgingBucketDto(string Bucket, int Count, decimal ValueUsd);

/// <summary>Same High/Medium/Low classification as AgedQuotationDto.RiskLevel, aggregated for the risk-level chart.</summary>
public sealed record RiskLevelBucketDto(string RiskLevel, int Count, decimal ValueUsd);

public sealed record AgedQuotationDto(
    Guid QuotationId,
    string QuotationNo,
    string BuyerName,
    string UnitName,
    decimal ValueUsd,
    int DaysOpen,
    string Status,
    string RiskLevel);
