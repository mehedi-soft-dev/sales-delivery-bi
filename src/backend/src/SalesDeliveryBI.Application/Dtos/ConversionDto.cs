namespace SalesDeliveryBI.Application.Dtos;

public sealed record ConversionDto(
    ConversionKpisDto Kpis,
    IReadOnlyList<MonthlyTrendEntryDto> MonthlyTrend,
    IReadOnlyList<BuyerPerformanceDto> BuyerPerformance);

public sealed record ConversionKpisDto(
    decimal ConversionRatePct,
    decimal WonValueUsd,
    decimal LostValueUsd,
    double AvgConversionDays);

public sealed record MonthlyTrendEntryDto(string Month, decimal ConversionRatePct);

public sealed record BuyerPerformanceDto(
    string BuyerName,
    int QuotationsCount,
    int WonCount,
    int LostCount,
    decimal ConversionRatePct,
    decimal ValueUsd);
