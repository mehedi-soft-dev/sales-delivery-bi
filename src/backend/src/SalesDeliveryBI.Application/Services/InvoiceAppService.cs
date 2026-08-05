using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Services;

/// <summary>Plain AppService, same convention as QuotationAppService/SalesOrderAppService — guard then cache-aside.</summary>
public class InvoiceAppService
{
    private static readonly IReadOnlyDictionary<string, Func<InvoiceRowDto, IComparable>> SortSelectors =
        new Dictionary<string, Func<InvoiceRowDto, IComparable>>
        {
            ["invoiceNo"] = r => r.InvoiceNo,
            ["invoiceDate"] = r => r.InvoiceDate,
            ["buyerName"] = r => r.BuyerName,
            ["unitName"] = r => r.UnitName,
            ["invoiceValueUsd"] = r => r.InvoiceValueUsd,
            ["outstandingUsd"] = r => r.OutstandingUsd,
            ["dueDate"] = r => r.DueDate,
            ["daysOverdue"] = r => r.DaysOverdue,
            ["arStatus"] = r => r.ArStatus,
        };

    private readonly IInvoiceRepository _repository;
    private readonly ICacheService _cache;
    private readonly IUnitAccessGuard _unitAccessGuard;
    private readonly CacheTtlOptions _cacheTtls;

    public InvoiceAppService(
        IInvoiceRepository repository,
        ICacheService cache,
        IUnitAccessGuard unitAccessGuard,
        CacheTtlOptions cacheTtls)
    {
        _repository = repository;
        _cache = cache;
        _unitAccessGuard = unitAccessGuard;
        _cacheTtls = cacheTtls;
    }

    public async Task<DashboardResponse<InvoiceResponseDto>> GetSummaryAsync(
        Guid? unitId,
        GridQuery grid,
        CancellationToken cancellationToken = default)
    {
        UnitScope scope = _unitAccessGuard.Validate(unitId, PermissionCodes.InvoiceViewAllUnits);

        DashboardResponse<InvoiceDto> cached = await _cache.GetOrSetAsync(
            CacheKeys.Invoice(scope),
            _cacheTtls.Invoice,
            ct => _repository.GetSummaryAsync(scope, ct),
            cancellationToken);

        PagedResult<InvoiceRowDto> page = GridPaging.Apply(cached.Data.Invoices, grid, SortSelectors);
        var response = new InvoiceResponseDto(cached.Data.Kpis, cached.Data.AgingBuckets, page);

        return new DashboardResponse<InvoiceResponseDto>(response, cached.LastRefresh);
    }
}
