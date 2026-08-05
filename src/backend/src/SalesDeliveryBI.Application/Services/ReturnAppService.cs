using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>Plain AppService, same convention as QuotationAppService/SalesOrderAppService — guard then cache-aside.</summary>
public class ReturnAppService
{
    private static readonly IReadOnlyDictionary<string, Func<ReturnRowDto, IComparable>> SortSelectors =
        new Dictionary<string, Func<ReturnRowDto, IComparable>>
        {
            ["returnNo"] = r => r.ReturnNo,
            ["returnDate"] = r => r.ReturnDate,
            ["buyerName"] = r => r.BuyerName,
            ["unitName"] = r => r.UnitName,
            ["returnValueUsd"] = r => r.ReturnValueUsd,
            ["returnQty"] = r => r.ReturnQty,
            ["reasonCode"] = r => r.ReasonCode,
        };

    private readonly IReturnRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;
    private readonly CacheTtlOptions _cacheTtls;

    public ReturnAppService(
        IReturnRepository repository,
        ICacheService cache,
        IUnitAccessGuard unitAccessGuard,
        CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
        _cacheTtls = cacheTtls;
    }

    public async Task<DashboardResponse<ReturnResponseDto>> GetSummaryAsync(
        Guid? unitId,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId, PermissionCodes.ReturnViewAllUnits);

        DashboardResponse<ReturnDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Return(scope),
            _cacheTtls.Return,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);

        PagedResult<ReturnRowDto> page = GridPaging.Apply(cached.Data.Returns, grid, SortSelectors);
        var response = new ReturnResponseDto(cached.Data.Kpis, cached.Data.ReasonBreakdown, page);

        return new DashboardResponse<ReturnResponseDto>(response, cached.LastRefresh);
    }
}
