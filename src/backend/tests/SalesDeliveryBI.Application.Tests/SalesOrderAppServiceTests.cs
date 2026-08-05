using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Application.Tests.Fakes;

namespace SalesDeliveryBI.Application.Tests;

/// <summary>Mocked repository/cache/guard — the real-DB/Redis path is covered separately in Infrastructure.Tests/Api.Tests.</summary>
public class SalesOrderAppServiceTests
{
    private static readonly CacheTtlOptions CacheTtls = new();
    private static readonly GridQuery DefaultGrid = new();

    [Fact]
    public async Task GetSummaryAsync_CallsGuardBeforeCache_AndRepositoryReceivesResolvedScope()
    {
        UnitScope resolvedScope = UnitScope.Unrestricted();
        var guard = new FakeUnitAccessGuard(_ => resolvedScope);
        var cache = new FakeCacheService();
        var repository = new FakeSalesOrderRepository();
        var appService = new SalesOrderAppService(repository, cache, guard, CacheTtls);

        DashboardResponse<SalesOrderResponseDto> result = await appService.GetSummaryAsync(unitId: null, DefaultGrid);

        Assert.Equal(1, guard.CallCount);
        Assert.Equal(1, cache.CallCount);
        Assert.Equal(1, repository.CallCount);
        Assert.Same(resolvedScope, repository.LastScope);
        Assert.Equal(CacheKeys.SalesOrder(resolvedScope), cache.LastKey);
        Assert.Equal(CacheTtls.SalesOrder, cache.LastTtl);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetSummaryAsync_GuardThrowsForbidden_PropagatesWithoutTouchingCacheOrRepository()
    {
        var guard = new FakeUnitAccessGuard(_ => throw new ForbiddenAccessException("outside assignment"));
        var cache = new FakeCacheService();
        var repository = new FakeSalesOrderRepository();
        var appService = new SalesOrderAppService(repository, cache, guard, CacheTtls);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => appService.GetSummaryAsync(Guid.NewGuid(), DefaultGrid));

        Assert.Equal(0, cache.CallCount);
        Assert.Equal(0, repository.CallCount);
    }

    [Fact]
    public async Task GetSummaryAsync_PagesAndSortsRowsFromTheCachedFullList()
    {
        var guard = new FakeUnitAccessGuard(_ => UnitScope.Unrestricted());
        var cache = new FakeCacheService();
        var repository = new FakeSalesOrderRepository
        {
            Orders =
            [
                new SalesOrderRowDto(Guid.NewGuid(), "SO-003", new DateOnly(2026, 7, 3), null, "Zara", "Fatema", "Unit-1",
                    300m, 0m, 300m, "Open", new DateOnly(2026, 8, 1)),
                new SalesOrderRowDto(Guid.NewGuid(), "SO-001", new DateOnly(2026, 7, 1), null, "H&M", "Jahid", "Unit-1",
                    100m, 0m, 100m, "Open", new DateOnly(2026, 8, 1)),
                new SalesOrderRowDto(Guid.NewGuid(), "SO-002", new DateOnly(2026, 7, 2), null, "Mango", "Mehedi", "Unit-1",
                    200m, 0m, 200m, "Open", new DateOnly(2026, 8, 1)),
            ],
        };
        var appService = new SalesOrderAppService(repository, cache, guard, CacheTtls);

        DashboardResponse<SalesOrderResponseDto> firstPage = await appService.GetSummaryAsync(
            null, new GridQuery(Page: 1, PageSize: 2, SortField: "orderValueUsd", SortDescending: false));

        Assert.Equal(3, firstPage.Data.Orders.TotalCount);
        Assert.Equal(2, firstPage.Data.Orders.Items.Count);
        Assert.Equal("SO-001", firstPage.Data.Orders.Items[0].SoNo);
        Assert.Equal("SO-002", firstPage.Data.Orders.Items[1].SoNo);

        DashboardResponse<SalesOrderResponseDto> secondPage = await appService.GetSummaryAsync(
            null, new GridQuery(Page: 2, PageSize: 2, SortField: "orderValueUsd", SortDescending: false));

        Assert.Single(secondPage.Data.Orders.Items);
        Assert.Equal("SO-003", secondPage.Data.Orders.Items[0].SoNo);
    }
}
