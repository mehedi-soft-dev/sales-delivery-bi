using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Application.Dtos;

/// <summary>Repository/cache-internal shape — Invoices is the FULL unpaged list, exactly what's cached under CacheKeys.Invoice.</summary>
public sealed record InvoiceDto(
    InvoiceKpisDto Kpis,
    IReadOnlyList<InvoiceAgingBucketDto> AgingBuckets,
    IReadOnlyList<InvoiceRowDto> Invoices);

/// <summary>What the API actually returns — Invoices is the server-side-paged slice, sliced from the cached full list in InvoiceAppService.</summary>
public sealed record InvoiceResponseDto(
    InvoiceKpisDto Kpis,
    IReadOnlyList<InvoiceAgingBucketDto> AgingBuckets,
    PagedResult<InvoiceRowDto> Invoices);

public sealed record InvoiceKpisDto(decimal TotalOutstandingUsd, decimal OverdueValueUsd, double AvgDaysSalesOutstanding);

/// <summary>Bucket = 'Current' (not yet overdue) / '1-30' / '31-60' / '60+' days overdue.</summary>
public sealed record InvoiceAgingBucketDto(string Bucket, int Count, decimal ValueUsd);

/// <summary>DaysOverdue/ArStatus are computed live off CURRENT_DATE (InvoiceRepository), not stored — same convention as Quotation's days_open.</summary>
public sealed record InvoiceRowDto(
    Guid InvoiceId,
    string InvoiceNo,
    DateOnly InvoiceDate,
    string BuyerName,
    string UnitName,
    decimal InvoiceValueUsd,
    decimal PaidAmountUsd,
    decimal OutstandingUsd,
    DateOnly DueDate,
    int DaysOverdue,
    string ArStatus);
