using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Infrastructure.Caching;
using SalesDeliveryBI.Infrastructure.Jobs;
using SalesDeliveryBI.Infrastructure.Persistence.Dapper;
using StackExchange.Redis;

namespace SalesDeliveryBI.Infrastructure.Tests;

/// <summary>Exercises the real dev Postgres + Redis (per local-environment.md) — not mocked, for the warm-up paths.</summary>
public class CacheWarmupJobTests
{
    private const string RedisConnectionString = "127.0.0.1:6381";

    private static CacheWarmupJob CreateJob(out IConnectionMultiplexer redis, IQuotationRepository? repositoryOverride = null)
    {
        redis = ConnectionMultiplexer.Connect(RedisConnectionString);
        var cache = new RedisCacheService(redis, NullLogger<RedisCacheService>.Instance);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SalesDeliveryBi"] =
                    "Host=127.0.0.1;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi;SSL Mode=Disable",
                ["Dashboards:HighValueThresholdAlertUsd"] = "100000",
            })
            .Build();

        IQuotationRepository repository = repositoryOverride ?? new QuotationRepository(new DapperContext(configuration), configuration);
        return new CacheWarmupJob(repository, cache, NullLogger<CacheWarmupJob>.Instance, new CacheTtlOptions());
    }

    [Fact]
    public async Task WarmUpAsync_PipelineMv_PopulatesTheSameKeyQuotationAppServiceReads()
    {
        CacheWarmupJob job = CreateJob(out IConnectionMultiplexer redis);
        string key = CacheKeys.Pipeline(UnitScope.Unrestricted(), includeDraft: false, null, null);
        string draftKey = CacheKeys.Pipeline(UnitScope.Unrestricted(), includeDraft: true, null, null);
        await redis.GetDatabase().KeyDeleteAsync(key);
        await redis.GetDatabase().KeyDeleteAsync(draftKey);

        await job.WarmUpAsync(CacheWarmupJob.SalesQuotationSummaryMv, CancellationToken.None);

        Assert.True(await redis.GetDatabase().KeyExistsAsync(key));
        Assert.True(await redis.GetDatabase().KeyExistsAsync(draftKey));
        redis.Dispose();
    }

    [Fact]
    public async Task WarmUpAsync_AgingMv_PopulatesTheSameKeyQuotationAppServiceReads()
    {
        CacheWarmupJob job = CreateJob(out IConnectionMultiplexer redis);
        string key = CacheKeys.Aging(UnitScope.Unrestricted(), includeDraft: false, null, null);
        string draftKey = CacheKeys.Aging(UnitScope.Unrestricted(), includeDraft: true, null, null);
        await redis.GetDatabase().KeyDeleteAsync(key);
        await redis.GetDatabase().KeyDeleteAsync(draftKey);

        await job.WarmUpAsync(CacheWarmupJob.QuotationPipelineDailyMv, CancellationToken.None);

        Assert.True(await redis.GetDatabase().KeyExistsAsync(key));
        Assert.True(await redis.GetDatabase().KeyExistsAsync(draftKey));
        redis.Dispose();
    }

    [Fact]
    public async Task WarmUpAsync_ConversionMv_PopulatesTheSameKeyQuotationAppServiceReads()
    {
        CacheWarmupJob job = CreateJob(out IConnectionMultiplexer redis);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        string key = CacheKeys.Conversion(UnitScope.Unrestricted(), monthStart, today);
        await redis.GetDatabase().KeyDeleteAsync(key);

        await job.WarmUpAsync(CacheWarmupJob.QuotationConversionRateMv, CancellationToken.None);

        Assert.True(await redis.GetDatabase().KeyExistsAsync(key));
        redis.Dispose();
    }

    [Fact]
    public async Task WarmUpAsync_UnknownMvName_DoesNotThrow()
    {
        CacheWarmupJob job = CreateJob(out IConnectionMultiplexer redis);

        Exception? exception = await Record.ExceptionAsync(() => job.WarmUpAsync("bi.mv_does_not_exist", CancellationToken.None));

        Assert.Null(exception);
        redis.Dispose();
    }

    [Fact]
    public async Task WarmUpAsync_RepositoryThrows_IsSwallowedNotPropagated()
    {
        var job = new CacheWarmupJob(new ThrowingQuotationRepository(), new RedisCacheService(
            ConnectionMultiplexer.Connect(RedisConnectionString), NullLogger<RedisCacheService>.Instance),
            NullLogger<CacheWarmupJob>.Instance, new CacheTtlOptions());

        Exception? exception = await Record.ExceptionAsync(
            () => job.WarmUpAsync(CacheWarmupJob.SalesQuotationSummaryMv, CancellationToken.None));

        Assert.Null(exception); // must never throw past the job boundary (checklist.md Phase 8)
    }

    private sealed class ThrowingQuotationRepository : IQuotationRepository
    {
        public Task<DashboardResponse<QuotationPipelineDto>> GetPipelineSummaryAsync(
            UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");

        public Task<DashboardResponse<ConversionDto>> GetConversionSummaryAsync(
            UnitScope scope, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");

        public Task<DashboardResponse<AgingDto>> GetAgingSummaryAsync(
            UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");

        public Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(Guid quotationId, UnitScope scope, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");

        public Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");

        public Task<IReadOnlyList<UnitOptionDto>> GetUnitsAsync(UnitScope scope, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Postgres failure");
    }
}
