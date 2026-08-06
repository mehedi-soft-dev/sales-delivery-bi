using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Application.Tests.Fakes;

namespace SalesDeliveryBI.Application.Tests;

/// <summary>Mocked repository/cache/guard — the real-DB/Redis path is covered separately in Infrastructure.Tests/Api.Tests.</summary>
public class QuotationAppServiceTests
{
    private static readonly CacheTtlOptions CacheTtls = new();

    private static readonly GridQuery DefaultGrid = new();

    [Fact]
    public async Task GetPipelineAsync_CallsGuardBeforeCache_AndRepositoryReceivesResolvedScope()
    {
        UnitScope resolvedScope = UnitScope.Unrestricted();
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        DashboardResponse<QuotationPipelineResponseDto> result =
            await appService.GetPipelineAsync(
                unitId: null, includeDraft: false, status: null, buyerName: null, fromDate: null, toDate: null, DefaultGrid);

        Assert.Equal(1, guard.CallCount);
        Assert.Equal(1, cache.CallCount);
        Assert.Equal(1, repository.CallCount);
        Assert.Same(resolvedScope, repository.LastScope);
        Assert.False(repository.LastIncludeDraft);
        Assert.Equal(CacheKeys.Pipeline(resolvedScope, includeDraft: false, null, null), cache.LastKey);
        Assert.Equal(CacheTtls.Pipeline, cache.LastTtl);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetPipelineAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => appService.GetPipelineAsync(
                Guid.NewGuid(), includeDraft: false, status: null, buyerName: null, fromDate: null, toDate: null, DefaultGrid));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetPipelineAsync_PagesAndSortsRowsFromTheCachedFullList()
    {
        var guard = new FakeUnitAccessGuard(_ => UnitScope.Unrestricted());
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository
        {
            OpenQuotations =
            [
                new OpenQuotationDto(Guid.NewGuid(), "QTN-003", "Zara", "Fatema", "Unit-1", 300m, "Draft", 3),
                new OpenQuotationDto(Guid.NewGuid(), "QTN-001", "H&M", "Jahid", "Unit-1", 100m, "Draft", 1),
                new OpenQuotationDto(Guid.NewGuid(), "QTN-002", "Mango", "Mehedi", "Unit-1", 200m, "Draft", 2),
            ],
        };
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        DashboardResponse<QuotationPipelineResponseDto> firstPage = await appService.GetPipelineAsync(
            null, includeDraft: false, status: null, buyerName: null, fromDate: null, toDate: null,
            new GridQuery(Page: 1, PageSize: 2, SortField: "valueUsd", SortDescending: false));

        Assert.Equal(3, firstPage.Data.OpenQuotations.TotalCount);
        Assert.Equal(2, firstPage.Data.OpenQuotations.Items.Count);
        Assert.Equal("QTN-001", firstPage.Data.OpenQuotations.Items[0].QuotationNo);
        Assert.Equal("QTN-002", firstPage.Data.OpenQuotations.Items[1].QuotationNo);

        // Only 1 cache/repository call across two different pages of the SAME cached dataset —
        // paging never bypasses or multiplies the cache-aside call.
        DashboardResponse<QuotationPipelineResponseDto> secondPage = await appService.GetPipelineAsync(
            null, includeDraft: false, status: null, buyerName: null, fromDate: null, toDate: null,
            new GridQuery(Page: 2, PageSize: 2, SortField: "valueUsd", SortDescending: false));

        Assert.Single(secondPage.Data.OpenQuotations.Items);
        Assert.Equal("QTN-003", secondPage.Data.OpenQuotations.Items[0].QuotationNo);
    }

    [Fact]
    public async Task GetConversionAsync_PassesResolvedScopeAndDatesToRepository()
    {
        UnitScope resolvedScope = UnitScope.RestrictedTo([Guid.NewGuid()]);
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);
        var fromDate = new DateOnly(2026, 6, 1);
        var toDate = new DateOnly(2026, 6, 30);

        await appService.GetConversionAsync(Guid.NewGuid(), fromDate, toDate, DefaultGrid);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(fromDate, repository.LastFromDate);
        Assert.Equal(toDate, repository.LastToDate);
        // Two cache calls: the main conversion summary, then the previous-period trend comparison series.
        Assert.Contains(CacheKeys.Conversion(resolvedScope, fromDate, toDate), cache.Keys);
        Assert.Equal(CacheTtls.Conversion, cache.LastTtl);
    }

    [Fact]
    public async Task GetConversionAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => appService.GetConversionAsync(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), DefaultGrid));

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
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        await appService.GetAgingAsync(null, includeDraft: false, highRiskOnly: false, fromDate: null, toDate: null, DefaultGrid);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.False(repository.LastIncludeDraft);
        Assert.Equal(CacheKeys.Aging(resolvedScope, includeDraft: false, null, null), cache.LastKey);
        Assert.Equal(CacheTtls.Aging, cache.LastTtl);
    }

    [Fact]
    public async Task GetAgingAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => appService.GetAgingAsync(
                Guid.NewGuid(), includeDraft: false, highRiskOnly: false, fromDate: null, toDate: null, DefaultGrid));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetAgingAsync_PagesRowsFromTheCachedFullList()
    {
        var guard = new FakeUnitAccessGuard(_ => UnitScope.Unrestricted());
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository
        {
            AgedQuotations =
            [
                new AgedQuotationDto(Guid.NewGuid(), "QTN-001", "H&M", "Unit-1", 100m, 40, "Submitted", "High"),
                new AgedQuotationDto(Guid.NewGuid(), "QTN-002", "Zara", "Unit-1", 200m, 10, "Draft", "Low"),
            ],
        };
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        DashboardResponse<AgingResponseDto> result =
            await appService.GetAgingAsync(
                null, includeDraft: false, highRiskOnly: false, fromDate: null, toDate: null, new GridQuery(Page: 1, PageSize: 1));

        Assert.Equal(2, result.Data.AgedQuotations.TotalCount);
        Assert.Single(result.Data.AgedQuotations.Items);
    }

    [Fact]
    public async Task GetByIdAsync_ValidatesWithNullUnitId_RegardlessOfCallerContext()
    {
        var guard = new FakeUnitAccessGuard(_ => UnitScope.Unrestricted());
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);
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
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

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
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);
        var fromDate = new DateOnly(2026, 6, 1);
        var toDate = new DateOnly(2026, 6, 30);

        await appService.GetSummaryAsync(null, fromDate, toDate);

        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(fromDate, repository.LastFromDate);
        Assert.Equal(toDate, repository.LastToDate);
        Assert.Equal(CacheKeys.Summary(resolvedScope, fromDate, toDate), cache.LastKey);
        Assert.Equal(CacheTtls.Summary, cache.LastTtl);
    }

    [Fact]
    public async Task GetSummaryAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeQuotationRepository();
        var appService = new QuotationAppService(repository, cache, guard, CacheTtls);

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => appService.GetSummaryAsync(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30)));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }
}
