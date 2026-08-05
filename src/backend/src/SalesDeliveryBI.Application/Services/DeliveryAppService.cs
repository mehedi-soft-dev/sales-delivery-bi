using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>Plain AppService, same convention as QuotationAppService/SalesOrderAppService — guard then cache-aside.</summary>
public class DeliveryAppService
{
    private static readonly IReadOnlyDictionary<string, Func<DeliveryRowDto, IComparable>> SortSelectors =
        new Dictionary<string, Func<DeliveryRowDto, IComparable>>
        {
            ["challanNo"] = r => r.ChallanNo,
            ["deliveryDate"] = r => r.DeliveryDate,
            ["soNo"] = r => r.SoNo,
            ["buyerName"] = r => r.BuyerName,
            ["unitName"] = r => r.UnitName,
            ["deliveredValueUsd"] = r => r.DeliveredValueUsd,
            ["delayDays"] = r => r.DelayDays,
            ["deliveryStatus"] = r => r.DeliveryStatus,
        };

    private readonly IDeliveryRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;
    private readonly CacheTtlOptions _cacheTtls;

    public DeliveryAppService(
        IDeliveryRepository repository,
        ICacheService cache,
        IUnitAccessGuard unitAccessGuard,
        CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
        _cacheTtls = cacheTtls;
    }

    public async Task<DashboardResponse<DeliveryResponseDto>> GetSummaryAsync(
        Guid? unitId,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId, PermissionCodes.DeliveryViewAllUnits);

        DashboardResponse<DeliveryDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Delivery(scope),
            _cacheTtls.Delivery,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);

        PagedResult<DeliveryRowDto> page = GridPaging.Apply(cached.Data.Deliveries, grid, SortSelectors);
        var response = new DeliveryResponseDto(cached.Data.Kpis, cached.Data.StatusBreakdown, page);

        return new DashboardResponse<DeliveryResponseDto>(response, cached.LastRefresh);
    }
}
