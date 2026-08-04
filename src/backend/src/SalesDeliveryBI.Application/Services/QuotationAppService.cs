using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>
/// Plain AppService, no MediatR (single-owner demo, 5 read-only endpoints — see docs/plans/backend/architecture.md).
/// Every method explicitly calls IUnitAccessGuard.Validate then ICacheService.GetOrSetAsync — by convention,
/// not structural enforcement, so a new method here must remember to do both.
///
/// Grid paging: the 3 dashboard methods below cache the FULL unpaged row list under the same unit+date-scoped
/// cache key as before (CacheKeys.cs, CacheWarmupJob.cs) — paging/sorting is applied in-memory via GridPaging
/// AFTER the cached fetch, never inside the cache key. This keeps the cache-warmup job's fixed warm set valid
/// for every page/sort combination a client requests, instead of multiplying cache entries per page.
/// </summary>
public class QuotationAppService
{
    private static readonly IReadOnlyDictionary<string, Func<OpenQuotationDto, IComparable>> PipelineSortSelectors =
        new Dictionary<string, Func<OpenQuotationDto, IComparable>>
        {
            ["quotationNo"] = r => r.QuotationNo,
            ["buyerName"] = r => r.BuyerName,
            ["merchandiserName"] = r => r.MerchandiserName,
            ["unitName"] = r => r.UnitName,
            ["valueUsd"] = r => r.ValueUsd,
            ["status"] = r => r.Status,
            ["daysOpen"] = r => r.DaysOpen,
        };

    private static readonly IReadOnlyDictionary<string, Func<BuyerPerformanceDto, IComparable>> BuyerPerformanceSortSelectors =
        new Dictionary<string, Func<BuyerPerformanceDto, IComparable>>
        {
            ["buyerName"] = r => r.BuyerName,
            ["quotationsCount"] = r => r.QuotationsCount,
            ["wonCount"] = r => r.WonCount,
            ["lostCount"] = r => r.LostCount,
            ["conversionRatePct"] = r => r.ConversionRatePct,
            ["valueUsd"] = r => r.ValueUsd,
        };

    private static readonly IReadOnlyDictionary<string, Func<AgedQuotationDto, IComparable>> AgedQuotationSortSelectors =
        new Dictionary<string, Func<AgedQuotationDto, IComparable>>
        {
            ["quotationNo"] = r => r.QuotationNo,
            ["buyerName"] = r => r.BuyerName,
            ["unitName"] = r => r.UnitName,
            ["valueUsd"] = r => r.ValueUsd,
            ["daysOpen"] = r => r.DaysOpen,
            ["status"] = r => r.Status,
            ["riskLevel"] = r => r.RiskLevel,
        };

    private readonly IQuotationRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;
    private readonly CacheTtlOptions _cacheTtls;

    public QuotationAppService(
        IQuotationRepository repository,
        ICacheService cache,
        IUnitAccessGuard unitAccessGuard,
        CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
        _cacheTtls = cacheTtls;
    }

    public async Task<DashboardResponse<QuotationPipelineResponseDto>> GetPipelineAsync(
        Guid? unitId,
        bool includeDraft,
        DateOnly? fromDate,
        DateOnly? toDate,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        DashboardResponse<QuotationPipelineDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Pipeline(scope, includeDraft, fromDate, toDate),
            _cacheTtls.Pipeline,
            ct => _repository.GetPipelineSummaryAsync(scope, includeDraft, fromDate, toDate, ct),
            cancellationToken);

        PagedResult<OpenQuotationDto> page = GridPaging.Apply(cached.Data.OpenQuotations, grid, PipelineSortSelectors);
        var response = new QuotationPipelineResponseDto(cached.Data.Kpis, cached.Data.StatusFunnel, page);

        return new DashboardResponse<QuotationPipelineResponseDto>(response, cached.LastRefresh);
    }

    public async Task<DashboardResponse<ConversionResponseDto>> GetConversionAsync(
        Guid? unitId,
        DateOnly fromDate,
        DateOnly toDate,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        DashboardResponse<ConversionDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Conversion(scope, fromDate, toDate),
            _cacheTtls.Conversion,
            ct => _repository.GetConversionSummaryAsync(scope, fromDate, toDate, ct),
            cancellationToken);

        PagedResult<BuyerPerformanceDto> page = GridPaging.Apply(cached.Data.BuyerPerformance, grid, BuyerPerformanceSortSelectors);
        var response = new ConversionResponseDto(cached.Data.Kpis, cached.Data.MonthlyTrend, page);

        return new DashboardResponse<ConversionResponseDto>(response, cached.LastRefresh);
    }

    public async Task<DashboardResponse<AgingResponseDto>> GetAgingAsync(
        Guid? unitId,
        bool includeDraft,
        DateOnly? fromDate,
        DateOnly? toDate,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        DashboardResponse<AgingDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Aging(scope, includeDraft, fromDate, toDate),
            _cacheTtls.Aging,
            ct => _repository.GetAgingSummaryAsync(scope, includeDraft, fromDate, toDate, ct),
            cancellationToken);

        PagedResult<AgedQuotationDto> page = GridPaging.Apply(cached.Data.AgedQuotations, grid, AgedQuotationSortSelectors);
        var response = new AgingResponseDto(cached.Data.Kpis, cached.Data.AgingBuckets, cached.Data.RiskLevels, page);

        return new DashboardResponse<AgingResponseDto>(response, cached.LastRefresh);
    }

    public async Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, CancellationToken cancellationToken = default)
    {
        // No unitId query param on this endpoint (api-contract.md #4) — access is enforced purely by whether
        // the resolved quotation's unit falls in the caller's scope; a miss returns null (404), never 403.
        UnitScope scope = _unitAccessGuard.Validate(null);

        return await _cache.GetOrSetAsync(
            CacheKeys.Detail(quotationId),
            _cacheTtls.Detail,
            ct => _repository.GetByIdAsync(quotationId, scope, ct),
            cancellationToken);
    }

    public async Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(Guid? unitId, CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId);

        return await _cache.GetOrSetAsync(
            CacheKeys.Summary(scope),
            _cacheTtls.Summary,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);
    }

    /// <summary>
    /// Units the caller may filter dashboards by — ALL units for a caller with bi.quotation.viewAllUnits,
    /// otherwise only their own assigned units. Never the full catalog for a row-restricted caller.
    /// </summary>
    public async Task<IReadOnlyList<UnitOptionDto>> GetUnitsAsync(CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(null);

        return await _cache.GetOrSetAsync(
            CacheKeys.Units(scope),
            _cacheTtls.Units,
            ct => _repository.GetUnitsAsync(scope, ct),
            cancellationToken);
    }
}
