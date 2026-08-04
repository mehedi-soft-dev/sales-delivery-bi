using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — OpenQuotations is the FULL unpaged list, exactly what's cached under CacheKeys.Pipeline.</summary>
public sealed record QuotationPipelineDto(
    PipelineKpisDto Kpis,
    IReadOnlyList<StatusFunnelEntryDto> StatusFunnel,
    IReadOnlyList<OpenQuotationDto> OpenQuotations);

/// <summary>What the API actually returns — OpenQuotations is the server-side-paged slice, sliced from the cached full list in QuotationAppService.</summary>
public sealed record QuotationPipelineResponseDto(
    PipelineKpisDto Kpis,
    IReadOnlyList<StatusFunnelEntryDto> StatusFunnel,
    PagedResult<OpenQuotationDto> OpenQuotations);

public sealed record PipelineKpisDto(
    int OpenQuotationsCount,
    decimal PipelineValueUsd,
    int PendingApprovalCount,
    double AvgDaysOpen);

public sealed record StatusFunnelEntryDto(string Status, int Count);

public sealed record OpenQuotationDto(
    Guid QuotationId,
    string QuotationNo,
    string BuyerName,
    string MerchandiserName,
    decimal ValueUsd,
    string Status,
    int DaysOpen);
