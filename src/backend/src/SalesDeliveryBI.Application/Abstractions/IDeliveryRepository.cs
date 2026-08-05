using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Application.Abstractions;

/// <summary>Reads bi.mv_delivery_performance via Dapper — a plain seeded table, same convention as ISalesOrderRepository.</summary>
public interface IDeliveryRepository
{
    Task<DashboardResponse<DeliveryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken);
}
