using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>Reads bi.* materialized views via Dapper (Infrastructure/Persistence/Dapper). No EF change-tracking on the read side.</summary>
public interface IQuotationRepository
{
    /// <summary>
    /// includeDraft=false (default): open quotations excluding Draft, and Draft is dropped from the status
    /// funnel entirely. includeDraft=true: Draft is folded back into the open set and funnel.
    /// fromDate/toDate filter by quotation_date — both null (the default) means unfiltered, every open
    /// quotation regardless of when it was raised.
    /// </summary>
    Task<DashboardResponse<QuotationPipelineDto>> GetPipelineSummaryAsync(
        UnitScope scope,
        bool includeDraft,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken);

    Task<DashboardResponse<ConversionDto>> GetConversionSummaryAsync(
        UnitScope scope,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);

    /// <summary>includeDraft/fromDate/toDate are the same semantics as GetPipelineSummaryAsync.</summary>
    Task<DashboardResponse<AgingDto>> GetAgingSummaryAsync(
        UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);

    /// <summary>Data is null when the quotation doesn't exist or falls outside <paramref name="scope"/> — caller maps this to 404, never 403.</summary>
    Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, UnitScope scope, CancellationToken cancellationToken);

    Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken);

    /// <summary>Units the caller may filter by — ALL units when <paramref name="scope"/> is unrestricted, otherwise only their assigned ones.</summary>
    Task<IReadOnlyList<UnitOptionDto>> GetUnitsAsync(UnitScope scope, CancellationToken cancellationToken);
}
