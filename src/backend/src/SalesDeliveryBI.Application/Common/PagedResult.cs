namespace SalesDeliveryBI.Application.Common;

/// <summary>One server-side-paged slice of a grid's rows, plus the total row count for the client's paginator.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
