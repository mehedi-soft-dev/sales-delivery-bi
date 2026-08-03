using Microsoft.Extensions.Logging;
using Quartz;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Infrastructure.Jobs;

/// <summary>
/// Fires ~10-15s after each MV's pg_cron refresh (one trigger per MV, see DependencyInjection.cs) and
/// re-populates Redis under the exact same keys QuotationAppService reads, so the first real dashboard
/// request after a refresh never pays the cache-miss cost. Talks to IQuotationRepository directly rather
/// than through QuotationAppService — a background job has no caller identity to run through
/// IUnitAccessGuard, so it warms the unrestricted (all-units) scope, the one every viewAllUnits caller hits.
/// A failed warm-up must never crash the host — the next real request just pays the miss cost once.
/// </summary>
public class CacheWarmupJob : IJob
{
    public const string MvNameDataKey = "MvName";

    public const string SalesQuotationSummaryMv = "bi.mv_sales_quotation_summary";
    public const string QuotationPipelineDailyMv = "bi.mv_quotation_pipeline_daily";
    public const string QuotationConversionRateMv = "bi.mv_quotation_conversion_rate";

    private static readonly UnitScope UnrestrictedScope = UnitScope.Unrestricted();

    private readonly IQuotationRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<CacheWarmupJob> _logger;

    public CacheWarmupJob(IQuotationRepository repository, ICacheService cache, ILogger<CacheWarmupJob> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public Task Execute(IJobExecutionContext context)
    {
        string mvName = context.MergedJobDataMap.GetString(MvNameDataKey)
            ?? throw new InvalidOperationException($"CacheWarmupJob requires a '{MvNameDataKey}' job data entry.");

        return WarmUpAsync(mvName, context.CancellationToken);
    }

    /// <summary>The directly-testable entry point — Execute(IJobExecutionContext) is just a thin Quartz adapter over this.</summary>
    public async Task WarmUpAsync(string mvName, CancellationToken cancellationToken)
    {
        try
        {
            await WarmAsync(mvName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warm-up failed for {MvName}", mvName);
        }
    }

    private Task WarmAsync(string mvName, CancellationToken cancellationToken) => mvName switch
    {
        SalesQuotationSummaryMv => WarmPipelineAsync(cancellationToken),
        QuotationPipelineDailyMv => WarmAgingAsync(cancellationToken),
        QuotationConversionRateMv => WarmConversionAsync(cancellationToken),
        _ => LogUnknownMvAsync(mvName),
    };

    private Task LogUnknownMvAsync(string mvName)
    {
        _logger.LogWarning("CacheWarmupJob triggered with unknown {MvName}", mvName);
        return Task.CompletedTask;
    }

    // Return type is deliberately the non-generic Task, not Task<DashboardResponse<T>> — WarmAsync's switch
    // expression above calls these three plus LogUnknownMvAsync as one common type; T differs per MV.
#pragma warning disable CA1859
    private Task WarmPipelineAsync(CancellationToken cancellationToken) => _cache.GetOrSetAsync(
        CacheKeys.Pipeline(UnrestrictedScope),
        DashboardCacheTtls.Pipeline,
        ct => _repository.GetPipelineSummaryAsync(UnrestrictedScope, ct),
        cancellationToken);

    private Task WarmAgingAsync(CancellationToken cancellationToken) => _cache.GetOrSetAsync(
        CacheKeys.Aging(UnrestrictedScope),
        DashboardCacheTtls.Aging,
        ct => _repository.GetAgingSummaryAsync(UnrestrictedScope, ct),
        cancellationToken);

    private Task WarmConversionAsync(CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return _cache.GetOrSetAsync(
            CacheKeys.Conversion(UnrestrictedScope, monthStart, today),
            DashboardCacheTtls.Conversion,
            ct => _repository.GetConversionSummaryAsync(UnrestrictedScope, monthStart, today, ct),
            cancellationToken);
    }
#pragma warning restore CA1859
}
