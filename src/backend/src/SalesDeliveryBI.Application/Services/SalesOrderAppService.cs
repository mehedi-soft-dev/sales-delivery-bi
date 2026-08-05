using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>
/// Plain AppService, same convention as QuotationAppService — explicitly calls IUnitAccessGuard.Validate
/// then ICacheService.GetOrSetAsync, by convention not structural enforcement.
/// </summary>
public class SalesOrderAppService
{
    private static readonly IReadOnlyDictionary<string, Func<SalesOrderRowDto, IComparable>> SortSelectors =
        new Dictionary<string, Func<SalesOrderRowDto, IComparable>>
        {
            ["soNo"] = r => r.SoNo,
            ["soDate"] = r => r.SoDate,
            ["buyerName"] = r => r.BuyerName,
            ["merchandiserName"] = r => r.MerchandiserName,
            ["unitName"] = r => r.UnitName,
            ["orderValueUsd"] = r => r.OrderValueUsd,
            ["deliveredValueUsd"] = r => r.DeliveredValueUsd,
            ["pendingValueUsd"] = r => r.PendingValueUsd,
            ["status"] = r => r.Status,
            ["promisedDeliveryDate"] = r => r.PromisedDeliveryDate,
        };

    private readonly ISalesOrderRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;
    private readonly CacheTtlOptions _cacheTtls;

    public SalesOrderAppService(
        ISalesOrderRepository repository,
        ICacheService cache,
        IUnitAccessGuard unitAccessGuard,
        CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
        _cacheTtls = cacheTtls;
    }

    public async Task<DashboardResponse<SalesOrderResponseDto>> GetSummaryAsync(
        Guid? unitId,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId, PermissionCodes.SalesOrderViewAllUnits);

        DashboardResponse<SalesOrderDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.SalesOrder(scope),
            _cacheTtls.SalesOrder,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);

        PagedResult<SalesOrderRowDto> page = GridPaging.Apply(cached.Data.Orders, grid, SortSelectors);
        var response = new SalesOrderResponseDto(cached.Data.Kpis, cached.Data.StatusBreakdown, page);

        return new DashboardResponse<SalesOrderResponseDto>(response, cached.LastRefresh);
    }
}
