using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — BuyerPerformance is the FULL unpaged list, exactly what's cached under CacheKeys.Conversion.</summary>
public sealed record ConversionDto(
    ConversionKpisDto Kpis,
    IReadOnlyList<MonthlyTrendEntryDto> MonthlyTrend,
    IReadOnlyList<BuyerPerformanceDto> BuyerPerformance);

/// <summary>What the API actually returns — BuyerPerformance is the server-side-paged slice, sliced from the cached full list in QuotationAppService.</summary>
public sealed record ConversionResponseDto(
    ConversionKpisDto Kpis,
    IReadOnlyList<MonthlyTrendEntryDto> MonthlyTrend,
    PagedResult<BuyerPerformanceDto> BuyerPerformance);

public sealed record ConversionKpisDto(
    decimal ConversionRatePct,
    decimal WonValueUsd,
    decimal LostValueUsd,
    double AvgConversionDays,
    int WonCount,
    int LostCount);

public sealed record MonthlyTrendEntryDto(string Month, decimal ConversionRatePct, int WonCount, int LostCount);

public sealed record BuyerPerformanceDto(
    string BuyerName,
    int QuotationsCount,
    int WonCount,
    int LostCount,
    decimal ConversionRatePct,
    decimal ValueUsd);
