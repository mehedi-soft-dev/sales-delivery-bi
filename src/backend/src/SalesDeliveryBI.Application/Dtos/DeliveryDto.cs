using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — Deliveries is the FULL unpaged list, exactly what's cached under CacheKeys.Delivery.</summary>
public sealed record DeliveryDto(
    DeliveryKpisDto Kpis,
    IReadOnlyList<DeliveryStatusBucketDto> StatusBreakdown,
    IReadOnlyList<DeliveryRowDto> Deliveries);

/// <summary>What the API actually returns — Deliveries is the server-side-paged slice, sliced from the cached full list in DeliveryAppService.</summary>
public sealed record DeliveryResponseDto(
    DeliveryKpisDto Kpis,
    IReadOnlyList<DeliveryStatusBucketDto> StatusBreakdown,
    PagedResult<DeliveryRowDto> Deliveries);

public sealed record DeliveryKpisDto(double OnTimeRatePct, int DelayedShipmentsCount, decimal DeliveredValueUsd);

/// <summary>'On-Time' / 'Late' per DeliveryRowDto.DeliveryStatus.</summary>
public sealed record DeliveryStatusBucketDto(string DeliveryStatus, int Count, decimal ValueUsd);

public sealed record DeliveryRowDto(
    Guid DeliveryId,
    string ChallanNo,
    DateOnly DeliveryDate,
    Guid SalesOrderId,
    string SoNo,
    string BuyerName,
    string UnitName,
    decimal DeliveredValueUsd,
    DateOnly PromisedDate,
    int DelayDays,
    string DeliveryStatus);
