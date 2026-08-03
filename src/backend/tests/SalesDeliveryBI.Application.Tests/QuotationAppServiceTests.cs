using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Application.Tests.Fakes;

namespace SalesDeliveryBI.Application.Tests;

/// <summary>Mocked repository/cache/guard — the real-DB/Redis path is covered separately in Infrastructure.Tests/Api.Tests.</summary>
public class QuotationAppServiceTests
{
    [Fact]
    public async Task GetPipelineAsync_CallsGuardBeforeCache_AndRepositoryReceivesResolvedScope()
    {
        UnitScope resolvedScope = UnitScope.Unrestricted();
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        DashboardResponse<QuotationPipelineDto> result = await appService.GetPipelineAsync(unitId: null);

        Assert.Equal(1, guard.CallCount);
        Assert.Equal(1, cache.CallCount);
        Assert.Equal(1, repository.CallCount);
        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(CacheKeys.Pipeline(resolvedScope), cache.LastKey);
        Assert.Equal(DashboardCacheTtls.Pipeline, cache.LastTtl);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetPipelineAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => appService.GetPipelineAsync(Guid.NewGuid()));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetConversionAsync_PassesResolvedScopeAndDatesToRepository()
    {
        UnitScope resolvedScope = UnitScope.RestrictedTo([Guid.NewGuid()]);
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);
        var fromDate = new DateOnly(2026, 6, 1);
        var toDate = new DateOnly(2026, 6, 30);

        await appService.GetConversionAsync(Guid.NewGuid(), fromDate, toDate);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(fromDate, repository.LastFromDate);
        Assert.Equal(toDate, repository.LastToDate);
        Assert.Equal(CacheKeys.Conversion(resolvedScope, fromDate, toDate), cache.LastKey);
        Assert.Equal(DashboardCacheTtls.Conversion, cache.LastTtl);
    }

    [Fact]
    public async Task GetConversionAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => appService.GetConversionAsync(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetAgingAsync_UsesAgingCacheKeyAndTtl()
    {
        UnitScope resolvedScope = UnitScope.Unrestricted();
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await appService.GetAgingAsync(null);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(CacheKeys.Aging(resolvedScope), cache.LastKey);
        Assert.Equal(DashboardCacheTtls.Aging, cache.LastTtl);
    }

    [Fact]
    public async Task GetAgingAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => appService.GetAgingAsync(Guid.NewGuid()));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetByIdAsync_ValidatesWithNullUnitId_RegardlessOfCallerContext()
    {
        var guard = new FakeUnitAccessGuard(_ => UnitScope.Unrestricted());
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);
        Guid quotationId = Guid.NewGuid();

        await appService.GetByIdAsync(quotationId);

        // {id} has no unitId query param (api-contract.md #4) — access is enforced purely by the resolved scope.
        Assert.Null(guard.LastRequestedUnitId);
        Assert.Equal(quotationId, repository.LastQuotationId);
        Assert.Equal(CacheKeys.Detail(quotationId), cache.LastKey);
    }

    [Fact]
    public async Task GetByIdAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => appService.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetSummaryAsync_UsesSummaryCacheKeyAndTtl()
    {
        UnitScope resolvedScope = UnitScope.Unrestricted();
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await appService.GetSummaryAsync(null);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(CacheKeys.Summary(resolvedScope), cache.LastKey);
        Assert.Equal(DashboardCacheTtls.Summary, cache.LastTtl);
    }

    [Fact]
    public async Task GetSummaryAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => appService.GetSummaryAsync(Guid.NewGuid()));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }
}
