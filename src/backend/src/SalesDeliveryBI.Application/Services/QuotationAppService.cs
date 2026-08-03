using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>
/// Plain AppService, no MediatR (single-owner demo, 5 read-only endpoints — see docs/plans/backend/architecture.md).
/// Every method explicitly calls IUnitAccessGuard.Validate then ICacheService.GetOrSetAsync — by convention,
/// not structural enforcement, so a new method here must remember to do both.
/// </summary>
public class QuotationAppService
{
    private static readonly TimeSpan PipelineTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan ConversionTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan AgingTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DetailTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SummaryTtl = TimeSpan.FromMinutes(5);

    private readonly IQuotationRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;

    public QuotationAppService(IQuotationRepository repository, ICacheService cache, IUnitAccessGuard unitAccessGuard)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
    }

    public async Task<DashboardResponse<QuotationPipelineDto>> GetPipelineAsync(Guid? unitId, CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        return await _cache.GetOrSetAsync(
            CacheKeys.Pipeline(scope),
            PipelineTtl,
            ct => _repository.GetPipelineSummaryAsync(scope, ct),
            cancellationToken);
    }

    public async Task<DashboardResponse<ConversionDto>> GetConversionAsync(
        Guid? unitId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        return await _cache.GetOrSetAsync(
            CacheKeys.Conversion(scope, fromDate, toDate),
            ConversionTtl,
            ct => _repository.GetConversionSummaryAsync(scope, fromDate, toDate, ct),
            cancellationToken);
    }

    public async Task<DashboardResponse<AgingDto>> GetAgingAsync(Guid? unitId, CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        return await _cache.GetOrSetAsync(
            CacheKeys.Aging(scope),
            AgingTtl,
            ct => _repository.GetAgingSummaryAsync(scope, ct),
            cancellationToken);
    }

    public async Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        // No unitId query param on this endpoint (api-contract.md #4) — access is enforced purely by whether
        // the resolved quotation's unit falls in the caller's scope; a miss returns null (404), never 403.
        UnitScope scope = _unitAccessGuard.Validate(null);

        return await _cache.GetOrSetAsync(
            CacheKeys.Detail(quotationId),
            DetailTtl,
            ct => _repository.GetByIdAsync(quotationId, scope, ct),
            cancellationToken);
    }

    public async Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(Guid? unitId, CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        return await _cache.GetOrSetAsync(
            CacheKeys.Summary(scope),
            SummaryTtl,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);
    }
}
