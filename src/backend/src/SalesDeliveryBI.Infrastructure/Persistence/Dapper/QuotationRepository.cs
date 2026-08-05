using System.Data;
using global::Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SalesDeliveryBI.Application.Abstractions;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

/// <summary>
/// Reads bi.* materialized views (Pipeline/Conversion/Aging dashboards) plus a couple of direct
/// sales.* lookups for the Quotation Detail view's non-MV fields (Incoterm/PaymentTerm/ValidUntil/Discount, items, history).
/// </summary>
public class QuotationRepository : IQuotationRepository
{
    private static readonly string[] PipelineFunnelOrder =
        ["Draft", "Submitted", "Negotiation", "PendingApproval", "Approved", "Converted"];

    private static readonly string[] AgingBucketOrder = ["0-7", "8-15", "16-30", "31-60", "60+"];

    private static readonly string[] RiskLevelOrder = ["High", "Medium", "Low"];

    private const string OpenStatusFilter = "status NOT IN ('Converted','Rejected','Expired')";

    /// <summary>
    /// includeDraft toggle for Pipeline/Aging: false = open quotations excluding Draft (default),
    /// true = open quotations including Draft (the plain OpenStatusFilter above).
    /// </summary>
    private const string OpenStatusFilterWithDraftToggle =
        "status NOT IN ('Converted','Rejected','Expired') AND (@IncludeDraft OR status <> 'Draft')";

    /// <summary>Optional quotation-date range for Pipeline/Aging — both bounds null (the default) means unfiltered.</summary>
    private const string DateRangeFilter =
        "(@FromDate IS NULL OR quotation_date >= @FromDate) AND (@ToDate IS NULL OR quotation_date <= @ToDate)";

    private const string LastRefreshSql =
        "SELECT MAX(finished_at) FROM bi.mv_refresh_log WHERE mv_name = @MvName AND status = 'SUCCESS'";

    private readonly DapperContext _dapperContext;
    private readonly decimal _highValueThresholdUsd;

    public QuotationRepository(DapperContext dapperContext, IConfiguration configuration)
    {
        _dapperContext = dapperContext;
        _highValueThresholdUsd = configuration.GetValue("Dashboards:HighValueThresholdAlertUsd", 100000m);
    }

    public async Task<DashboardResponse<QuotationPipelineDto>> GetPipelineSummaryAsync(
        UnitScope scope,
        bool includeDraft,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);
        parameters.Add("IncludeDraft", includeDraft);
        // Explicit DbType required: a null DateOnly? gives Npgsql no CLR type info to infer the
        // Postgres parameter type from, and "@FromDate IS NULL" alone isn't enough context either —
        // without this, a null date filter fails with "could not determine data type of parameter".
        parameters.Add("FromDate", fromDate, DbType.Date);
        parameters.Add("ToDate", toDate, DbType.Date);

        PipelineKpisDto kpis = await connection.QuerySingleAsync<PipelineKpisDto>(
            new CommandDefinition(PipelineKpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<StatusFunnelEntryDto> funnelRows = await connection.QueryAsync<StatusFunnelEntryDto>(
            new CommandDefinition(PipelineFunnelSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<OpenQuotationDto> openQuotations = await connection.QueryAsync<OpenQuotationDto>(
            new CommandDefinition(PipelineOpenQuotationsSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_quotation_summary", cancellationToken);

        var dto = new QuotationPipelineDto(kpis, BuildOrderedFunnel(funnelRows, includeDraft), openQuotations.ToList());
        return new DashboardResponse<QuotationPipelineDto>(dto, lastRefresh);
    }

    public async Task<DashboardResponse<ConversionDto>> GetConversionSummaryAsync(
        UnitScope scope,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);
        parameters.Add("FromDate", fromDate);
        parameters.Add("ToDate", toDate);

        ConversionKpisDto kpis = await connection.QuerySingleAsync<ConversionKpisDto>(
            new CommandDefinition(ConversionKpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<MonthlyTrendEntryDto> trend = await connection.QueryAsync<MonthlyTrendEntryDto>(
            new CommandDefinition(ConversionTrendSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<BuyerPerformanceDto> buyerPerformance = await connection.QueryAsync<BuyerPerformanceDto>(
            new CommandDefinition(ConversionBuyerPerformanceSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<LostReasonBreakdownDto> lostReasons = await connection.QueryAsync<LostReasonBreakdownDto>(
            new CommandDefinition(ConversionLostReasonSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_quotation_conversion_rate", cancellationToken);

        var dto = new ConversionDto(kpis, trend.ToList(), buyerPerformance.ToList(), lostReasons.ToList());
        return new DashboardResponse<ConversionDto>(dto, lastRefresh);
    }

    public async Task<IReadOnlyList<MonthlyTrendEntryDto>> GetMonthlyTrendAsync(
        UnitScope scope, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);
        parameters.Add("FromDate", fromDate);
        parameters.Add("ToDate", toDate);

        IEnumerable<MonthlyTrendEntryDto> trend = await connection.QueryAsync<MonthlyTrendEntryDto>(
            new CommandDefinition(ConversionTrendSql, parameters, cancellationToken: cancellationToken));

        return trend.ToList();
    }

    public async Task<DashboardResponse<AgingDto>> GetAgingSummaryAsync(
        UnitScope scope, bool includeDraft, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);
        parameters.Add("IncludeDraft", includeDraft);
        parameters.Add("FromDate", fromDate, DbType.Date);
        parameters.Add("ToDate", toDate, DbType.Date);

        AgingKpisDto kpis = await connection.QuerySingleAsync<AgingKpisDto>(
            new CommandDefinition(AgingKpisSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<AgingBucketDto> bucketRows = await connection.QueryAsync<AgingBucketDto>(
            new CommandDefinition(AgingBucketsSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<RiskLevelBucketDto> riskLevelRows = await connection.QueryAsync<RiskLevelBucketDto>(
            new CommandDefinition(AgingRiskLevelSql, parameters, cancellationToken: cancellationToken));

        IEnumerable<AgedQuotationDto> agedQuotations = await connection.QueryAsync<AgedQuotationDto>(
            new CommandDefinition(AgedQuotationsSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_quotation_summary", cancellationToken);

        var dto = new AgingDto(kpis, BuildOrderedBuckets(bucketRows), BuildOrderedRiskLevels(riskLevelRows), agedQuotations.ToList());
        return new DashboardResponse<AgingDto>(dto, lastRefresh);
    }

    public async Task<DashboardResponse<QuotationDetailDto?>> GetByIdAsync(
        Guid quotationId,
        UnitScope scope,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);
        parameters.Add("QuotationId", quotationId);

        QuotationHeaderRow? header = await connection.QueryFirstOrDefaultAsync<QuotationHeaderRow>(
            new CommandDefinition(QuotationHeaderSql, parameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_quotation_summary", cancellationToken);

        if (header is null)
        {
            return new DashboardResponse<QuotationDetailDto?>(null, lastRefresh);
        }

        QuotationDetailDto dto = await BuildDetailDtoAsync(connection, header, quotationId, cancellationToken);
        return new DashboardResponse<QuotationDetailDto?>(dto, lastRefresh);
    }

    public async Task<DashboardResponse<QuotationSummaryDto>> GetSummaryAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        decimal openPipelineValueUsd = await connection.QuerySingleAsync<decimal>(
            new CommandDefinition(SummaryPipelineValueSql, parameters, cancellationToken: cancellationToken));

        decimal conversionRateMtdPct = await connection.QuerySingleAsync<decimal>(
            new CommandDefinition(SummaryConversionRateMtdSql, parameters, cancellationToken: cancellationToken));

        DynamicParameters alertParameters = ScopeParams(scope);
        alertParameters.Add("HighValueThresholdUsd", _highValueThresholdUsd);
        int highValueAgedAlertCount = await connection.QuerySingleAsync<int>(
            new CommandDefinition(SummaryHighValueAgedAlertCountSql, alertParameters, cancellationToken: cancellationToken));

        DateTime lastRefresh = await GetLastRefreshAsync(connection, "bi.mv_sales_quotation_summary", cancellationToken);

        var dto = new QuotationSummaryDto(openPipelineValueUsd, conversionRateMtdPct, highValueAgedAlertCount);
        return new DashboardResponse<QuotationSummaryDto>(dto, lastRefresh);
    }

    public async Task<IReadOnlyList<UnitOptionDto>> GetUnitsAsync(UnitScope scope, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = ScopeParams(scope);

        IEnumerable<UnitOptionDto> units = await connection.QueryAsync<UnitOptionDto>(
            new CommandDefinition(UnitsSql, parameters, cancellationToken: cancellationToken));

        return units.ToList();
    }

    private static async Task<QuotationDetailDto> BuildDetailDtoAsync(
        NpgsqlConnection connection,
        QuotationHeaderRow header,
        Guid quotationId,
        CancellationToken cancellationToken)
    {
        var idParam = new { QuotationId = quotationId };

        QuotationOltpRow oltp = await connection.QuerySingleAsync<QuotationOltpRow>(
            new CommandDefinition(QuotationOltpSql, idParam, cancellationToken: cancellationToken));

        decimal fxRate = oltp.Value == 0 ? 1m : header.QuotationValueUsd / oltp.Value;
        decimal discountUsd = Math.Round(oltp.Discount * fxRate, 2);
        decimal subtotalUsd = header.QuotationValueUsd + discountUsd;

        IEnumerable<QuotationItemDto> items = await connection.QueryAsync<QuotationItemDto>(
            new CommandDefinition(QuotationItemsSql, idParam, cancellationToken: cancellationToken));

        IEnumerable<QuotationStatusHistoryDto> history = await connection.QueryAsync<QuotationStatusHistoryDto>(
            new CommandDefinition(QuotationStatusHistorySql, idParam, cancellationToken: cancellationToken));

        return new QuotationDetailDto(
            header.QuotationId, header.QuotationNo, header.QuotationDate, header.BuyerName, header.MerchandiserName,
            header.UnitName, header.StyleNo, header.Season, header.CurrencyCode, header.QuotationValueUsd,
            oltp.Incoterm, oltp.PaymentTerm, oltp.ValidUntil, discountUsd, subtotalUsd,
            header.Status, header.StatusDate, header.DaysInStatus, header.DaysOpen,
            header.ConvertedToSoNo, header.ConvertedDate, header.ConversionDays, header.LostReason, header.CreatedBy,
            items.ToList(), history.ToList());
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

    /// <summary>Draft is dropped from the funnel entirely (not just zeroed) when includeDraft is false.</summary>
    private static List<StatusFunnelEntryDto> BuildOrderedFunnel(IEnumerable<StatusFunnelEntryDto> rows, bool includeDraft)
    {
        Dictionary<string, StatusFunnelEntryDto> byStatus = rows.ToDictionary(r => r.Status);
        IEnumerable<string> statuses = includeDraft ? PipelineFunnelOrder : PipelineFunnelOrder.Where(status => status != "Draft");
        return statuses.Select(status => byStatus.GetValueOrDefault(status) ?? new StatusFunnelEntryDto(status, 0, 0)).ToList();
    }

    private static List<RiskLevelBucketDto> BuildOrderedRiskLevels(IEnumerable<RiskLevelBucketDto> rows)
    {
        Dictionary<string, RiskLevelBucketDto> byRiskLevel = rows.ToDictionary(r => r.RiskLevel);
        return RiskLevelOrder.Select(level => byRiskLevel.GetValueOrDefault(level) ?? new RiskLevelBucketDto(level, 0, 0)).ToList();
    }

    private static List<AgingBucketDto> BuildOrderedBuckets(IEnumerable<AgingBucketDto> rows)
    {
        Dictionary<string, AgingBucketDto> byBucket = rows.ToDictionary(r => r.Bucket);
        return AgingBucketOrder.Select(b => byBucket.GetValueOrDefault(b) ?? new AgingBucketDto(b, 0, 0)).ToList();
    }

    private static async Task<DateTime> GetLastRefreshAsync(NpgsqlConnection connection, string mvName, CancellationToken cancellationToken)
    {
        DateTime? lastRefresh = await connection.QuerySingleOrDefaultAsync<DateTime?>(
            new CommandDefinition(LastRefreshSql, new { MvName = mvName }, cancellationToken: cancellationToken));
        return lastRefresh ?? DateTime.MinValue;
    }

    private const string PipelineKpisSql = $$"""
        SELECT
            COUNT(*) FILTER (WHERE {{OpenStatusFilterWithDraftToggle}})::int AS OpenQuotationsCount,
            COALESCE(SUM(quotation_value_usd) FILTER (WHERE {{OpenStatusFilterWithDraftToggle}}), 0) AS PipelineValueUsd,
            COUNT(*) FILTER (WHERE status = 'PendingApproval')::int AS PendingApprovalCount,
            COALESCE(AVG(days_open) FILTER (WHERE {{OpenStatusFilterWithDraftToggle}}), 0)::float8 AS AvgDaysOpen
        FROM bi.mv_sales_quotation_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        """;

    private const string PipelineFunnelSql = $$"""
        SELECT status AS Status, COUNT(*)::int AS Count, COALESCE(SUM(quotation_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_quotation_summary
        WHERE status IN ('Draft','Submitted','Negotiation','PendingApproval','Approved','Converted')
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        GROUP BY status
        """;

    private const string PipelineOpenQuotationsSql = $$"""
        SELECT quotation_id AS QuotationId, quotation_no AS QuotationNo, buyer_name AS BuyerName,
               merchandiser_name AS MerchandiserName, unit_name AS UnitName,
               quotation_value_usd AS ValueUsd, status AS Status, days_open AS DaysOpen
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilterWithDraftToggle}}
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        ORDER BY quotation_date DESC
        """;

    private const string ConversionKpisSql = """
        SELECT
            COALESCE(ROUND(100.0 * SUM(won_count) / NULLIF(SUM(quotations_count),0), 2), 0) AS ConversionRatePct,
            COALESCE(SUM(won_value_usd), 0) AS WonValueUsd,
            COALESCE(SUM(lost_value_usd), 0) AS LostValueUsd,
            COALESCE(AVG(avg_conversion_days), 0)::float8 AS AvgConversionDays,
            COALESCE(SUM(won_count), 0)::int AS WonCount,
            COALESCE(SUM(lost_count), 0)::int AS LostCount
        FROM bi.mv_quotation_conversion_rate
        WHERE month BETWEEN date_trunc('month', @FromDate) AND date_trunc('month', @ToDate)
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string ConversionTrendSql = """
        SELECT to_char(month, 'YYYY-MM') AS Month,
               COALESCE(ROUND(100.0 * SUM(won_count) / NULLIF(SUM(quotations_count),0), 2), 0) AS ConversionRatePct,
               COALESCE(SUM(won_count), 0)::int AS WonCount,
               COALESCE(SUM(lost_count), 0)::int AS LostCount
        FROM bi.mv_quotation_conversion_rate
        WHERE month BETWEEN date_trunc('month', @FromDate) AND date_trunc('month', @ToDate)
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY month
        ORDER BY month
        """;

    private const string ConversionBuyerPerformanceSql = """
        SELECT buyer_name AS BuyerName,
               SUM(quotations_count)::int AS QuotationsCount,
               SUM(won_count)::int AS WonCount,
               SUM(lost_count)::int AS LostCount,
               COALESCE(ROUND(100.0 * SUM(won_count) / NULLIF(SUM(quotations_count),0), 2), 0) AS ConversionRatePct,
               COALESCE(SUM(quotation_value_usd), 0) AS ValueUsd
        FROM bi.mv_quotation_conversion_rate
        WHERE month BETWEEN date_trunc('month', @FromDate) AND date_trunc('month', @ToDate)
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY buyer_name
        ORDER BY ValueUsd DESC
        """;

    /// <summary>
    /// Win/Loss reason analysis (docs/requirements §4.2) — LOST_REASON only exists on the row-level
    /// mv_sales_quotation_summary (§3.1), not the pre-aggregated mv_quotation_conversion_rate, so this
    /// reads the detail MV directly, scoped to the same Rejected/Expired quotations that make up "Lost".
    /// </summary>
    private const string ConversionLostReasonSql = """
        SELECT COALESCE(lost_reason, 'Unspecified') AS Reason, COUNT(*)::int AS Count, COALESCE(SUM(quotation_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_quotation_summary
        WHERE status IN ('Rejected', 'Expired')
          AND quotation_date BETWEEN @FromDate AND @ToDate
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        GROUP BY Reason
        ORDER BY ValueUsd DESC
        """;

    private const string AgingKpisSql = $$"""
        SELECT
            COALESCE(SUM(quotation_value_usd) FILTER (WHERE {{OpenStatusFilterWithDraftToggle}}), 0) AS TotalOpenValueUsd,
            COALESCE(SUM(quotation_value_usd) FILTER (WHERE {{OpenStatusFilterWithDraftToggle}} AND days_open > 30), 0) AS HighRiskAgedValueUsd
        FROM bi.mv_sales_quotation_summary
        WHERE (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        """;

    private const string AgingBucketsSql = $$"""
        SELECT
            CASE
                WHEN days_open BETWEEN 0 AND 7 THEN '0-7'
                WHEN days_open BETWEEN 8 AND 15 THEN '8-15'
                WHEN days_open BETWEEN 16 AND 30 THEN '16-30'
                WHEN days_open BETWEEN 31 AND 60 THEN '31-60'
                ELSE '60+'
            END AS Bucket,
            COUNT(*)::int AS Count,
            COALESCE(SUM(quotation_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilterWithDraftToggle}}
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        GROUP BY Bucket
        """;

    private const string AgingRiskLevelSql = $$"""
        SELECT
            CASE WHEN days_open > 30 THEN 'High' WHEN days_open > 15 THEN 'Medium' ELSE 'Low' END AS RiskLevel,
            COUNT(*)::int AS Count,
            COALESCE(SUM(quotation_value_usd), 0) AS ValueUsd
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilterWithDraftToggle}}
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        GROUP BY RiskLevel
        """;

    private const string AgedQuotationsSql = $$"""
        SELECT quotation_id AS QuotationId, quotation_no AS QuotationNo, buyer_name AS BuyerName,
               unit_name AS UnitName, quotation_value_usd AS ValueUsd, days_open AS DaysOpen, status AS Status,
               CASE WHEN days_open > 30 THEN 'High' WHEN days_open > 15 THEN 'Medium' ELSE 'Low' END AS RiskLevel
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilterWithDraftToggle}}
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
          AND {{DateRangeFilter}}
        ORDER BY days_open DESC
        """;

    private const string SummaryPipelineValueSql = $$"""
        SELECT COALESCE(SUM(quotation_value_usd), 0)
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilter}}
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string SummaryConversionRateMtdSql = """
        SELECT COALESCE(ROUND(100.0 * SUM(won_count) / NULLIF(SUM(quotations_count),0), 2), 0)
        FROM bi.mv_quotation_conversion_rate
        WHERE month = date_trunc('month', CURRENT_DATE)
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string SummaryHighValueAgedAlertCountSql = $$"""
        SELECT COUNT(*)::int
        FROM bi.mv_sales_quotation_summary
        WHERE {{OpenStatusFilter}}
          AND days_open > 15
          AND quotation_value_usd > @HighValueThresholdUsd
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string UnitsSql = """
        SELECT "Id" AS Id, "UnitName" AS Name
        FROM sales."Units"
        WHERE (@Unrestricted OR "Id" = ANY(@UnitIds))
        ORDER BY "UnitName"
        """;

    private const string QuotationHeaderSql = """
        SELECT quotation_id AS QuotationId, quotation_no AS QuotationNo, quotation_date AS QuotationDate,
               buyer_name AS BuyerName, merchandiser_name AS MerchandiserName, unit_name AS UnitName,
               style_no AS StyleNo, season AS Season, currency_code AS CurrencyCode, quotation_value_usd AS QuotationValueUsd,
               status AS Status, status_date AS StatusDate, days_in_status AS DaysInStatus, days_open AS DaysOpen,
               converted_to_so_no AS ConvertedToSoNo, converted_date AS ConvertedDate, conversion_days AS ConversionDays,
               lost_reason AS LostReason, created_by AS CreatedBy
        FROM bi.mv_sales_quotation_summary
        WHERE quotation_id = @QuotationId
          AND (@Unrestricted OR unit_id = ANY(@UnitIds))
        """;

    private const string QuotationOltpSql = """
        SELECT "Value" AS Value, "Discount" AS Discount, "Incoterm" AS Incoterm, "PaymentTerm" AS PaymentTerm, "ValidUntil" AS ValidUntil
        FROM sales."Quotations"
        WHERE "Id" = @QuotationId
        """;

    private const string QuotationItemsSql = """
        SELECT "StyleNo" AS StyleNo, "ItemDescription" AS ItemDescription, "Qty" AS Qty, "UnitPrice" AS UnitPrice, "Amount" AS Amount
        FROM sales."QuotationItems"
        WHERE "QuotationId" = @QuotationId
        ORDER BY "StyleNo"
        """;

    private const string QuotationStatusHistorySql = """
        SELECT "Status" AS Status, "StatusDate" AS StatusDate
        FROM sales."QuotationStatusHistories"
        WHERE "QuotationId" = @QuotationId
        ORDER BY "StatusDate"
        """;

    private sealed record QuotationHeaderRow(
        Guid QuotationId, string QuotationNo, DateOnly QuotationDate, string BuyerName, string MerchandiserName,
        string UnitName, string StyleNo, string Season, string CurrencyCode, decimal QuotationValueUsd,
        string Status, DateTime StatusDate, int DaysInStatus, int DaysOpen,
        string? ConvertedToSoNo, DateTime? ConvertedDate, int? ConversionDays, string? LostReason, Guid CreatedBy);

    private sealed record QuotationOltpRow(decimal Value, decimal Discount, string Incoterm, string PaymentTerm, DateOnly ValidUntil);
}
