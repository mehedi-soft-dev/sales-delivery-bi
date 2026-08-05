using global::Dapper;
using Npgsql;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>
/// Reads bi.mv_sales_invoice_summary — a plain seeded table, same convention as SalesOrderRepository.
/// ArStatus/DaysOverdue are computed live off CURRENT_DATE in every query below, never stored — same
/// pattern as Quotation's days_open (QuotationRepository) — so "Overdue" naturally becomes true as real
/// time passes a seeded due_date, without needing any refresh step.
/// </summary>
public class InvoiceRepository : IInvoiceRepository
{
    private static readonly string[] AgingBucketOrder = ["Current", "1-30", "31-60", "60+"];

    private const string LastRefreshSql =
        "SELECT MAX(finished_at) FROM bi.mv_refresh_log WHERE mv_name = @MvName AND status = 'SUCCESS'";

    private readonly DapperContext _dapperContext;

    public InvoiceRepository(DapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<DashboardResponse<InvoiceDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        InvoiceKpisDto kpis = await connection.QuerySingleAsync<InvoiceKpisDto>(
            new CommandDefinition(KpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<InvoiceAgingBucketDto> agingBuckets = await connection.QueryAsync<InvoiceAgingBucketDto>(
            new CommandDefinition(AgingBucketsSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<InvoiceRowDto> invoices = await connection.QueryAsync<InvoiceRowDto>(
            new CommandDefinition(InvoicesSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_invoice_summary", cancellationToken);

        var dto = new InvoiceDto(kpis, BuildOrderedBuckets(agingBuckets), invoices.ToList());
        return new DashboardResponse<InvoiceDto>(dto, lastRefresh);
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

    private static List<InvoiceAgingBucketDto> BuildOrderedBuckets(IEnumerable<InvoiceAgingBucketDto> rows)
    {
        Dictionary<string, InvoiceAgingBucketDto> byBucket = rows.ToDictionary(r => r.Bucket);
        return AgingBucketOrder.Select(b => byBucket.GetValueOrDefault(b) ?? new InvoiceAgingBucketDto(b, 0, 0)).ToList();
    }

    private const string KpisSql = """
        SELECT
            COALESCE(SUM(invoice_value_usd - paid_amount_usd) FILTER (WHERE paid_amount_usd < invoice_value_usd), 0) AS TotalOutstandingUsd,
            COALESCE(SUM(invoice_value_usd - paid_amount_usd)
                FILTER (WHERE paid_amount_usd < invoice_value_usd AND due_date < CURRENT_DATE), 0) AS OverdueValueUsd,
            COALESCE(AVG(CURRENT_DATE - invoice_date) FILTER (WHERE paid_amount_usd < invoice_value_usd), 0)::float8 AS AvgDaysSalesOutstanding
        FROM bi.mv_sales_invoice_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string AgingBucketsSql = """
        SELECT
            CASE
                WHEN due_date >= CURRENT_DATE THEN 'Current'
                WHEN CURRENT_DATE - due_date <= 30 THEN '1-30'
                WHEN CURRENT_DATE - due_date <= 60 THEN '31-60'
                ELSE '60+'
            END AS Bucket,
            COUNT(*)::int AS Count,
            COALESCE(SUM(invoice_value_usd - paid_amount_usd), 0) AS ValueUsd
        FROM bi.mv_sales_invoice_summary
        WHERE paid_amount_usd < invoice_value_usd
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY 1
        """;

    private const string InvoicesSql = """
        SELECT invoice_id AS InvoiceId, invoice_no AS InvoiceNo, invoice_date AS InvoiceDate,
               buyer_name AS BuyerName, unit_name AS UnitName, invoice_value_usd AS InvoiceValueUsd,
               paid_amount_usd AS PaidAmountUsd, (invoice_value_usd - paid_amount_usd) AS OutstandingUsd, due_date AS DueDate,
               GREATEST(CURRENT_DATE - due_date, 0)::int AS DaysOverdue,
               CASE
                   WHEN paid_amount_usd >= invoice_value_usd THEN 'Paid'
                   WHEN CURRENT_DATE > due_date THEN 'Overdue'
                   ELSE 'Current'
               END AS ArStatus
        FROM bi.mv_sales_invoice_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
        ORDER BY invoice_date DESC
        """;
}
