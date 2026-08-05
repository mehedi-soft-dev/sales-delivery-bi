using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — Returns is the FULL unpaged list, exactly what's cached under CacheKeys.Return.</summary>
public sealed record ReturnDto(
    ReturnKpisDto Kpis,
    IReadOnlyList<ReturnReasonBreakdownDto> ReasonBreakdown,
    IReadOnlyList<ReturnRowDto> Returns);

/// <summary>What the API actually returns — Returns is the server-side-paged slice, sliced from the cached full list in ReturnAppService.</summary>
public sealed record ReturnResponseDto(
    ReturnKpisDto Kpis,
    IReadOnlyList<ReturnReasonBreakdownDto> ReasonBreakdown,
    PagedResult<ReturnRowDto> Returns);

/// <summary>ReturnRatePct = SUM(return_value_usd) / SUM(invoice_value_usd across all invoices) — "return impact on revenue".</summary>
public sealed record ReturnKpisDto(double ReturnRatePct, decimal ReturnValueUsd);

public sealed record ReturnReasonBreakdownDto(string ReasonCode, int Count, decimal ValueUsd);

public sealed record ReturnRowDto(
    Guid ReturnId,
    string ReturnNo,
    DateOnly ReturnDate,
    string BuyerName,
    string UnitName,
    decimal ReturnValueUsd,
    int ReturnQty,
    string ReasonCode);
