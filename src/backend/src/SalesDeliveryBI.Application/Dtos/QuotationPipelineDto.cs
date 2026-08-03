namespace SalesDeliveryBI.Application.Dtos;

public sealed record QuotationPipelineDto(
    PipelineKpisDto Kpis,
    IReadOnlyList<StatusFunnelEntryDto> StatusFunnel,
    IReadOnlyList<OpenQuotationDto> OpenQuotations);

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
