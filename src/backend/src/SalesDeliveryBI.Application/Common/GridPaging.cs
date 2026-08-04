namespace SalesDeliveryBI.Application.Common;

/// <summary>
/// Applies a <see cref="GridQuery"/> to an in-memory row list. `sortSelectors` is a per-caller allow-list of
/// client-facing field names — an unrecognized `SortField` is a safe no-op (falls back to the source's existing
/// order) rather than throwing, since this is a display-ordering preference, not a data-integrity concern.
/// </summary>
public static class GridPaging
{
    public static PagedResult<T> Apply<T>(
        IReadOnlyList<T> source,
        GridQuery query,
        IReadOnlyDictionary<string, Func<T, IComparable>> sortSelectors)
    {
        IEnumerable<T> ordered = source;

        if (query.SortField is not null && sortSelectors.TryGetValue(query.SortField, out Func<T, IComparable>? selector))
        {
            ordered = query.SortDescending ? source.OrderByDescending(selector) : source.OrderBy(selector);
        }

        int page = query.NormalizedPage;
        int pageSize = query.NormalizedPageSize;

        List<T> items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<T>(items, source.Count, page, pageSize);
    }
}
