using global::Dapper;
using Npgsql;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>
/// Reads bi.mv_sales_order_summary — a plain seeded table (DatabaseSeeder), not a pg_cron-refreshed
/// materialized view: there's no OLTP source for this module to refresh from. "Last refresh" still comes
/// from bi.mv_refresh_log like every other dashboard — the seeder inserts a row there each time it runs.
/// </summary>
public class SalesOrderRepository : ISalesOrderRepository
{
    private const string LastRefreshSql =
        "SELECT MAX(finished_at) FROM bi.mv_refresh_log WHERE mv_name = @MvName AND status = 'SUCCESS'";

    private readonly DapperContext _dapperContext;

    public SalesOrderRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<DashboardResponse<SalesOrderDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        SalesOrderKpisDto kpis = await connection.QuerySingleAsync<SalesOrderKpisDto>(
            new CommandDefinition(KpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<SalesOrderStatusBucketDto> statusBreakdown = await connection.QueryAsync<SalesOrderStatusBucketDto>(
            new CommandDefinition(StatusBreakdownSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<SalesOrderRowDto> orders = await connection.QueryAsync<SalesOrderRowDto>(
            new CommandDefinition(OrdersSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_order_summary", cancellationToken);

        var dto = new SalesOrderDto(kpis, statusBreakdown.ToList(), orders.ToList());
        return new DashboardResponse<SalesOrderDto>(dto, lastRefresh);
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
            COALESCE(SUM(pending_value_usd) FILTER (WHERE status <> 'Closed'), 0) AS OpenBacklogValueUsd,
            COUNT(*) FILTER (WHERE status <> 'Closed')::int AS OrderCount,
            COALESCE(AVG(promised_delivery_date - so_date) FILTER (WHERE status <> 'Closed'), 0)::float8 AS AvgOrderToPromisedDeliveryDays
        FROM bi.mv_sales_order_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string StatusBreakdownSql = """
        SELECT status AS Status, COUNT(*)::int AS Count, COALESCE(SUM(order_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_order_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY status
        """;

    private const string OrdersSql = """
        SELECT so_id AS SoId, so_no AS SoNo, so_date AS SoDate, quotation_id AS QuotationId,
               buyer_name AS BuyerName, merchandiser_name AS MerchandiserName, unit_name AS UnitName,
               order_value_usd AS OrderValueUsd, delivered_value_usd AS DeliveredValueUsd, pending_value_usd AS PendingValueUsd,
               status AS Status, promised_delivery_date AS PromisedDeliveryDate
        FROM bi.mv_sales_order_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        ORDER BY so_date DESC
        """;
}
