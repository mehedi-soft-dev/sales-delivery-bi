using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace SalesDeliveryBI.Infrastructure.Jobs;

/// <summary>One CacheWarmupJob trigger per MV, offset ~15s after that MV's pg_cron cadence (see database/schema-plan.md).</summary>
public static class CacheWarmupScheduling
{
    public static IServiceCollection AddCacheWarmupJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            AddWarmupJob(q, CacheWarmupJob.SalesQuotationSummaryMv, "0/3", "PipelineWarmup");
            AddWarmupJob(q, CacheWarmupJob.QuotationPipelineDailyMv, "0/15", "AgingWarmup");
            AddWarmupJob(q, CacheWarmupJob.QuotationConversionRateMv, "0/15", "ConversionWarmup");
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = false);

        return services;
    }

    private static void AddWarmupJob(
        IServiceCollectionQuartzConfigurator quartz,
        string mvName,
        string minuteInterval,
        string keyName)
    {
        var jobKey = new JobKey(keyName);

        quartz.AddJob<CacheWarmupJob>(jobKey, job => job
            .UsingJobData(CacheWarmupJob.MvNameDataKey, mvName)
            .StoreDurably());

        quartz.AddTrigger(trigger => trigger
            .ForJob(jobKey)
            .WithIdentity($"{keyName}-trigger")
            .WithCronSchedule($"15 {minuteInterval} * * * ?"));
    }
}
