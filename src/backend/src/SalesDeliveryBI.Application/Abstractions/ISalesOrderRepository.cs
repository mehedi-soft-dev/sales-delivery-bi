using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>
/// Reads bi.mv_sales_order_summary via Dapper (Infrastructure/Persistence/Dapper) — a plain seeded table,
/// not a real materialized view (docs/plans, Sales Order module plan: no OLTP source exists for this module,
/// so there's nothing to refresh from; "last refresh" is when DatabaseSeeder last (re-)seeded it).
/// </summary>
public interface ISalesOrderRepository
{
    Task<DashboardResponse<SalesOrderDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken);
}
