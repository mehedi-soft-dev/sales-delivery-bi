using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>Reads bi.* materialized views via Dapper (Infrastructure/Persistence/Dapper). No EF change-tracking on the read side.</summary>
public interface IQuotationRepository
{
    Task<DashboardResponse<QuotationPipelineDto>> GetPipelineSummaryAsync(UnitScope scope, CancellationToken cancellationToken);

    Task<DashboardResponse<ConversionDto>> GetConversionSummaryAsync(
        UnitScope scope,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);

    Task<DashboardResponse<AgingDto>> GetAgingSummaryAsync(UnitScope scope, CancellationToken cancellationToken);

    /// <summary>Data is null when the quotation doesn't exist or falls outside <paramref name="scope"/> — caller maps this to 404, never 403.</summary>
    Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, UnitScope scope, CancellationToken cancellationToken);

    Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken);
}
