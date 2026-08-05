using global::Dapper;
using Npgsql;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>
/// Reads bi.mv_sales_return_summary — a plain seeded table, same convention as SalesOrderRepository.
/// ReturnRatePct is "return value as a share of all invoiced value" (docs/requirements: "return impact on
/// revenue"), so its KPI query also scopes bi.mv_sales_invoice_summary for the denominator.
/// </summary>
public class ReturnRepository : IReturnRepository
{
    private const string LastRefreshSql =
        "SELECT MAX(finished_at) FROM bi.mv_refresh_log WHERE mv_name = @MvName AND status = 'SUCCESS'";

    private readonly DapperContext _dapperContext;

    public ReturnRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<DashboardResponse<ReturnDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        ReturnKpisDto kpis = await connection.QuerySingleAsync<ReturnKpisDto>(
            new CommandDefinition(KpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<ReturnReasonBreakdownDto> reasonBreakdown = await connection.QueryAsync<ReturnReasonBreakdownDto>(
            new CommandDefinition(ReasonBreakdownSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<ReturnRowDto> returns = await connection.QueryAsync<ReturnRowDto>(
            new CommandDefinition(ReturnsSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_return_summary", cancellationToken);

        var dto = new ReturnDto(kpis, reasonBreakdown.ToList(), returns.ToList());
        return new DashboardResponse<ReturnDto>(dto, lastRefresh);
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
        WITH returns_scope AS (
            SELECT * FROM bi.mv_sales_return_summary WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        ), invoices_scope AS (
            SELECT * FROM bi.mv_sales_invoice_summary WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        )
        SELECT
            COALESCE(ROUND(100.0 * (SELECT COALESCE(SUM(return_value_usd), 0) FROM returns_scope)
                / NULLIF((SELECT SUM(invoice_value_usd) FROM invoices_scope), 0), 2), 0)::float8 AS ReturnRatePct,
            (SELECT COALESCE(SUM(return_value_usd), 0) FROM returns_scope) AS ReturnValueUsd
        """;

    private const string ReasonBreakdownSql = """
        SELECT reason_code AS ReasonCode, COUNT(*)::int AS Count, COALESCE(SUM(return_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_return_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY reason_code
        ORDER BY ValueUsd DESC
        """;

    private const string ReturnsSql = """
        SELECT return_id AS ReturnId, return_no AS ReturnNo, return_date AS ReturnDate, buyer_name AS BuyerName,
               unit_name AS UnitName, return_value_usd AS ReturnValueUsd, return_qty AS ReturnQty, reason_code AS ReasonCode
        FROM bi.mv_sales_return_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        ORDER BY return_date DESC
        """;
}
