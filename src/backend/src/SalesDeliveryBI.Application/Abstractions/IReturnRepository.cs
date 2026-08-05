using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>Reads bi.mv_sales_return_summary via Dapper — a plain seeded table, same convention as ISalesOrderRepository.</summary>
public interface IReturnRepository
{
    Task<DashboardResponse<ReturnDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken);
}
