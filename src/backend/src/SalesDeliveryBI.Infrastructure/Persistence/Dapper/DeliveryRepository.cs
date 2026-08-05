using global::Dapper;
using Npgsql;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>Reads bi.mv_delivery_performance — a plain seeded table, same convention as SalesOrderRepository.</summary>
public class DeliveryRepository : IDeliveryRepository
{
    private const string LastRefreshSql =
        "SELECT MAX(finished_at) FROM bi.mv_refresh_log WHERE mv_name = @MvName AND status = 'SUCCESS'";

    private readonly DapperContext _dapperContext;

    public DeliveryRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<DashboardResponse<DeliveryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        DeliveryKpisDto kpis = await connection.QuerySingleAsync<DeliveryKpisDto>(
            new CommandDefinition(KpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<DeliveryStatusBucketDto> statusBreakdown = await connection.QueryAsync<DeliveryStatusBucketDto>(
            new CommandDefinition(StatusBreakdownSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<DeliveryRowDto> deliveries = await connection.QueryAsync<DeliveryRowDto>(
            new CommandDefinition(DeliveriesSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_delivery_performance", cancellationToken);

        var dto = new DeliveryDto(kpis, statusBreakdown.ToList(), deliveries.ToList());
        return new DashboardResponse<DeliveryDto>(dto, lastRefresh);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = _dapperContext.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DynamicParameters ScopeParams(UnitScope scope)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Unrestricted", scope.IsUnrestricted);
        parameters.Add("UnitIds", scope.UnitIds.ToArray());
        return parameters;
    }

    private static async Task<DateTime> GetLastRefreshAsync(NpgsqlConnection connection, string mvName, CancellationToken cancellationToken)
    {
        DateTime? lastRefresh = await connection.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(LastRefreshSql, new { MvName = mvName }, cancellationToken: cancellationToken));
        return lastRefresh ?? DateTime.MinValue;
    }

    private const string KpisSql = """
        SELECT
            COALESCE(ROUND(100.0 * COUNT(*) FILTER (WHERE delivery_status = 'On-Time') / NULLIF(COUNT(*), 0), 2), 0)::float8 AS OnTimeRatePct,
            COUNT(*) FILTER (WHERE delivery_status = 'Late')::int AS DelayedShipmentsCount,
            COALESCE(SUM(delivered_value_usd), 0) AS DeliveredValueUsd
        FROM bi.mv_delivery_performance
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string StatusBreakdownSql = """
        SELECT delivery_status AS DeliveryStatus, COUNT(*)::int AS Count, COALESCE(SUM(delivered_value_usd), 0) AS ValueUsd
        FROM bi.mv_delivery_performance
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY delivery_status
        """;

    private const string DeliveriesSql = """
        SELECT d.delivery_id AS DeliveryId, d.challan_no AS ChallanNo, d.delivery_date AS DeliveryDate,
               d.sales_order_id AS SalesOrderId, so.so_no AS SoNo, d.buyer_name AS BuyerName, d.unit_name AS UnitName,
               d.delivered_value_usd AS DeliveredValueUsd, d.promised_date AS PromisedDate, d.delay_days AS DelayDays,
               d.delivery_status AS DeliveryStatus
        FROM bi.mv_delivery_performance d
        JOIN bi.mv_sales_order_summary so ON so.so_id = d.sales_order_id
        WHERE (@Unrestricted OR d.unit_id = ANY(@UnitIds))
        ORDER BY d.delivery_date DESC
        """;
}
