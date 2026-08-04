using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Infrastructure.Persistence.Dapper;

namespace SalesDeliveryBI.Infrastructure.Tests;

/// <summary>
/// Runs against the real dev Postgres (docs/plans/backend/local-environment.md), seeded via DatabaseSeeder
/// (docs/plans/database/seed-data.md) — not mocked, per checklist.md Phase 5.
/// </summary>
public class QuotationRepositoryTests
{
    private const string ConnectionString =
        "Host=127.0.0.1;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi;SSL Mode=Disable";

    private static readonly UnitScope Unrestricted = UnitScope.Unrestricted();

    private static QuotationRepository CreateRepository()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SalesDeliveryBi"] = ConnectionString,
                ["Dashboards:HighValueThresholdAlertUsd"] = "100000",
            })
            .Build();

        return new QuotationRepository(new DapperContext(configuration), configuration);
    }

    private static async Task<Guid> FindQuotationIdAsync(string quotationNo)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        Guid? id = await connection.QuerySingleOrDefaultAsync<Guid?>(
            """SELECT "Id" FROM sales."Quotations" WHERE "QuotationNo" = @QuotationNo""",
            new { QuotationNo = quotationNo });

        return id ?? throw new InvalidOperationException($"Seeded quotation '{quotationNo}' not found.");
    }

    [Fact]
    public async Task GetPipelineSummaryAsync_ExcludingDraft_MatchesSeededStatusCounts()
    {
        QuotationRepository repository = CreateRepository();

        DashboardResponse<QuotationPipelineDto> result =
            await repository.GetPipelineSummaryAsync(Unrestricted, includeDraft: false, null, null, CancellationToken.None);

        // Seed status distribution (seed-data.md): Approved 3, Negotiation 3, PendingApproval 1, Submitted 5 = 12 open excluding Draft.
        Assert.Equal(12, result.Data.Kpis.OpenQuotationsCount);
        Assert.Equal(1, result.Data.Kpis.PendingApprovalCount);
        Assert.True(result.Data.Kpis.PipelineValueUsd > 0);

        // Draft is dropped from the funnel entirely (not zeroed) when includeDraft is false.
        Dictionary<string, int> funnel = result.Data.StatusFunnel.ToDictionary(f => f.Status, f => f.Count);
        Assert.False(funnel.ContainsKey("Draft"));
        Assert.Equal(5, funnel["Submitted"]);
        Assert.Equal(3, funnel["Negotiation"]);
        Assert.Equal(1, funnel["PendingApproval"]);
        Assert.Equal(3, funnel["Approved"]);
        Assert.Equal(7, funnel["Converted"]);

        Assert.Equal(12, result.Data.OpenQuotations.Count);
        Assert.DoesNotContain(result.Data.OpenQuotations, q => q.Status == "Draft");
        Assert.True(result.LastRefresh > DateTime.MinValue);
    }

    [Fact]
    public async Task GetPipelineSummaryAsync_IncludingDraft_AddsDraftBackIntoOpenSetAndFunnel()
    {
        QuotationRepository repository = CreateRepository();

        DashboardResponse<QuotationPipelineDto> result =
            await repository.GetPipelineSummaryAsync(Unrestricted, includeDraft: true, null, null, CancellationToken.None);

        // 12 (excluding Draft) + 8 Draft = 20 total open, same as the pre-toggle "open" definition.
        Assert.Equal(20, result.Data.Kpis.OpenQuotationsCount);
        Assert.Equal(20, result.Data.OpenQuotations.Count);
        Assert.Contains(result.Data.OpenQuotations, q => q.Status == "Draft");

        Dictionary<string, int> funnel = result.Data.StatusFunnel.ToDictionary(f => f.Status, f => f.Count);
        Assert.Equal(8, funnel["Draft"]);
    }

    [Fact]
    public async Task GetPipelineSummaryAsync_DateRangeFilter_ExcludesQuotationsOutsideRange()
    {
        QuotationRepository repository = CreateRepository();

        // Seed dates span 2026-06-12..2026-08-01 (seed-data.md) — a July-only window excludes every June row.
        var fromDate = new DateOnly(2026, 7, 1);
        var toDate = new DateOnly(2026, 7, 31);

        DashboardResponse<QuotationPipelineDto> filtered =
            await repository.GetPipelineSummaryAsync(Unrestricted, includeDraft: true, fromDate, toDate, CancellationToken.None);
        DashboardResponse<QuotationPipelineDto> unfiltered =
            await repository.GetPipelineSummaryAsync(Unrestricted, includeDraft: true, null, null, CancellationToken.None);

        Assert.True(filtered.Data.OpenQuotations.Count < unfiltered.Data.OpenQuotations.Count);
    }

    [Fact]
    public async Task GetConversionSummaryAsync_CoversAllSeededQuotations()
    {
        QuotationRepository repository = CreateRepository();
        var fromDate = new DateOnly(2026, 6, 1);
        var toDate = new DateOnly(2026, 8, 31);

        DashboardResponse<ConversionDto> result =
            await repository.GetConversionSummaryAsync(Unrestricted, fromDate, toDate, CancellationToken.None);

        int totalQuotations = result.Data.BuyerPerformance.Sum(b => b.QuotationsCount);
        Assert.Equal(30, totalQuotations);

        int totalWon = result.Data.BuyerPerformance.Sum(b => b.WonCount);
        Assert.Equal(7, totalWon); // 7 Converted rows in the seed set

        Assert.NotEmpty(result.Data.MonthlyTrend);
    }

    [Fact]
    public async Task GetAgingSummaryAsync_ExcludingDraft_BucketsReconcileWithOpenCount()
    {
        QuotationRepository repository = CreateRepository();

        DashboardResponse<AgingDto> result =
            await repository.GetAgingSummaryAsync(Unrestricted, includeDraft: false, null, null, CancellationToken.None);

        Assert.Equal(5, result.Data.AgingBuckets.Count);
        Assert.Equal(12, result.Data.AgingBuckets.Sum(b => b.Count));
        Assert.Equal(12, result.Data.AgedQuotations.Count);
        Assert.DoesNotContain(result.Data.AgedQuotations, q => q.Status == "Draft");
        Assert.True(result.Data.Kpis.HighRiskAgedValueUsd <= result.Data.Kpis.TotalOpenValueUsd);
    }

    [Fact]
    public async Task GetAgingSummaryAsync_IncludingDraft_AddsDraftBackIntoBucketsAndGrid()
    {
        QuotationRepository repository = CreateRepository();

        DashboardResponse<AgingDto> result =
            await repository.GetAgingSummaryAsync(Unrestricted, includeDraft: true, null, null, CancellationToken.None);

        Assert.Equal(20, result.Data.AgingBuckets.Sum(b => b.Count));
        Assert.Equal(20, result.Data.AgedQuotations.Count);
        Assert.Contains(result.Data.AgedQuotations, q => q.Status == "Draft");
    }

    [Fact]
    public async Task GetByIdAsync_ItemsSumReconcilesWithHeaderValue_AndHasStatusHistory()
    {
        QuotationRepository repository = CreateRepository();
        Guid quotationId = await FindQuotationIdAsync("QTN-2026-0001"); // Converted

        DashboardResponse<QuotationDetailDto?> result =
            await repository.GetByIdAsync(quotationId, Unrestricted, CancellationToken.None);

        Assert.NotNull(result.Data);
        QuotationDetailDto detail = result.Data!;
        Assert.Equal("QTN-2026-0001", detail.QuotationNo);
        Assert.Equal(2, detail.Items.Count);
        Assert.Equal(detail.SubtotalUsd - detail.DiscountUsd, detail.QuotationValueUsd);
        Assert.NotEmpty(detail.StatusHistory);
        Assert.Equal("Converted", detail.StatusHistory[^1].Status);
    }

    [Fact]
    public async Task GetByIdAsync_OutsideScope_ReturnsNullData()
    {
        QuotationRepository repository = CreateRepository();
        Guid quotationId = await FindQuotationIdAsync("QTN-2026-0001");
        UnitScope emptyScope = UnitScope.RestrictedTo([Guid.NewGuid()]);

        DashboardResponse<QuotationDetailDto?> result =
            await repository.GetByIdAsync(quotationId, emptyScope, CancellationToken.None);

        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsNonNegativeKpis()
    {
        QuotationRepository repository = CreateRepository();

        DashboardResponse<QuotationSummaryDto> result = await repository.GetSummaryAsync(Unrestricted, CancellationToken.None);

        Assert.True(result.Data.OpenPipelineValueUsd > 0);
        Assert.True(result.Data.HighValueAgedAlertCount >= 0);
    }
}
