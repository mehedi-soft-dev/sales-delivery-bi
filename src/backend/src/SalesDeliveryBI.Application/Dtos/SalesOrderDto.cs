using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — Orders is the FULL unpaged list, exactly what's cached under CacheKeys.SalesOrder.</summary>
public sealed record SalesOrderDto(
    SalesOrderKpisDto Kpis,
    IReadOnlyList<SalesOrderStatusBucketDto> StatusBreakdown,
    IReadOnlyList<SalesOrderRowDto> Orders);

/// <summary>What the API actually returns — Orders is the server-side-paged slice, sliced from the cached full list in SalesOrderAppService.</summary>
public sealed record SalesOrderResponseDto(
    SalesOrderKpisDto Kpis,
    IReadOnlyList<SalesOrderStatusBucketDto> StatusBreakdown,
    PagedResult<SalesOrderRowDto> Orders);

public sealed record SalesOrderKpisDto(decimal OpenBacklogValueUsd, int OrderCount, double AvgOrderToPromisedDeliveryDays);

public sealed record SalesOrderStatusBucketDto(string Status, int Count, decimal ValueUsd);

public sealed record SalesOrderRowDto(
    Guid SoId,
    string SoNo,
    DateOnly SoDate,
    Guid? QuotationId,
    string BuyerName,
    string MerchandiserName,
    string UnitName,
    decimal OrderValueUsd,
    decimal DeliveredValueUsd,
    decimal PendingValueUsd,
    string Status,
    DateOnly PromisedDeliveryDate);
