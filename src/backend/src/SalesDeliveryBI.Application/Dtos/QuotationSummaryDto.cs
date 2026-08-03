namespace SalesDeliveryBI.Application.Dtos;

public sealed record QuotationSummaryDto(
    decimal OpenPipelineValueUsd,
    decimal ConversionRateMtdPct,
    int HighValueAgedAlertCount);
