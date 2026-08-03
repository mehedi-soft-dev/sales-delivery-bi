using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Tests.Fakes;

/// <summary>Records the scope/params QuotationAppService resolved and passed down, so tests can assert on them.</summary>
internal sealed class FakeQuotationRepository : IQuotationRepository
{
    public UnitScope? LastScope { get; private set; }
    public DateOnly? LastFromDate { get; private set; }
    public DateOnly? LastToDate { get; private set; }
    public Guid? LastQuotationId { get; private set; }
    public int CallCount { get; private set; }

    public Task<DashboardResponse<QuotationPipelineDto>> GetPipelineSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        LastScope = scope;
        CallCount++;
        var dto = new QuotationPipelineDto(new PipelineKpisDto(0, 0m, 0, 0d), [], []);
        return Task.FromResult(new DashboardResponse<QuotationPipelineDto>(dto, DateTime.UtcNow));
    }

    public Task<DashboardResponse<ConversionDto>> GetConversionSummaryAsync(
        UnitScope scope, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        LastScope = scope;
        LastFromDate = fromDate;
        LastToDate = toDate;
        CallCount++;
        var dto = new ConversionDto(new ConversionKpisDto(0m, 0m, 0m, 0d), [], []);
        return Task.FromResult(new DashboardResponse<ConversionDto>(dto, DateTime.UtcNow));
    }

    public Task<DashboardResponse<AgingDto>> GetAgingSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        LastScope = scope;
        CallCount++;
        var dto = new AgingDto(new AgingKpisDto(0m, 0m), [], []);
        return Task.FromResult(new DashboardResponse<AgingDto>(dto, DateTime.UtcNow));
    }

    public Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, UnitScope scope, CancellationToken cancellationToken)
    {
        LastScope = scope;
        LastQuotationId = quotationId;
        CallCount++;
        return Task.FromResult(new DashboardResponse<QuotationDetailDto?>(null, DateTime.UtcNow));
    }

    public Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        LastScope = scope;
        CallCount++;
        var dto = new QuotationSummaryDto(0m, 0m, 0);
        return Task.FromResult(new DashboardResponse<QuotationSummaryDto>(dto, DateTime.UtcNow));
    }
}
