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
    public const string QuotationConversionRateMv = "bi.mv_quotation_conversion_rate";

    private static readonly UnitScope UnrestrictedScope = UnitScope.Unrestricted();
    private static readonly bool[] FalseThenTrue = [false, true];

    private readonly IQuotationRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<CacheWarmupJob> _logger;
    private readonly CacheTtlOptions _cacheTtls;

    public CacheWarmupJob(IQuotationRepository repository, ICacheService cache, ILogger<CacheWarmupJob> logger, CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
        _cacheTtls = cacheTtls;
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

    // Pipeline and Aging both read bi.mv_sales_quotation_summary (QuotationRepository.cs), so both warm off
    // its refresh trigger — there used to be a separate trigger keyed to bi.mv_quotation_pipeline_daily for
    // Aging, but that MV was dropped (it was never queried by anything and, being a materialized view with
    // no append/history mechanism, couldn't have served its intended "daily snapshot" purpose anyway); its
    // pg_cron cadence had nothing to do with Aging's real data dependency.
    private Task WarmAsync(string mvName, CancellationToken cancellationToken) => mvName switch
    {
        SalesQuotationSummaryMv => WarmPipelineAndAgingAsync(cancellationToken),
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
    private async Task WarmPipelineAndAgingAsync(CancellationToken cancellationToken)
    {
        await WarmPipelineAsync(cancellationToken);
        await WarmAgingAsync(cancellationToken);
    }

    // Only the unfiltered (fromDate/toDate = null) variant is warmed — dates are arbitrary user-chosen
    // input, same reason WarmConversionAsync only warms the current month rather than every possible range.
    private async Task WarmPipelineAsync(CancellationToken cancellationToken)
    {
        foreach (bool includeDraft in FalseThenTrue)
        {
            await _cache.GetOrSetAsync(
                CacheKeys.Pipeline(UnrestrictedScope, includeDraft, null, null),
                _cacheTtls.Pipeline,
                ct => _repository.GetPipelineSummaryAsync(UnrestrictedScope, includeDraft, null, null, ct),
                cancellationToken);
        }
    }

    private async Task WarmAgingAsync(CancellationToken cancellationToken)
    {
        foreach (bool includeDraft in FalseThenTrue)
        {
            await _cache.GetOrSetAsync(
                CacheKeys.Aging(UnrestrictedScope, includeDraft, null, null),
                _cacheTtls.Aging,
                ct => _repository.GetAgingSummaryAsync(UnrestrictedScope, includeDraft, null, null, ct),
                cancellationToken);
        }
    }

    private Task WarmConversionAsync(CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return _cache.GetOrSetAsync(
            CacheKeys.Conversion(UnrestrictedScope, monthStart, today),
            _cacheTtls.Conversion,
            ct => _repository.GetConversionSummaryAsync(UnrestrictedScope, monthStart, today, ct),
            cancellationToken);
    }
#pragma warning restore CA1859
}
