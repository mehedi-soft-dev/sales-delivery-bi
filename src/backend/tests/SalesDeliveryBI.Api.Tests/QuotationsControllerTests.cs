using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Npgsql;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Tests;

/// <summary>
/// End-to-end against the real dev Postgres/Redis (docs/plans/backend/local-environment.md) through the
/// actual HTTP pipeline (JWT auth, authorization policies, exception handling) — nothing mocked.
/// </summary>
public class QuotationsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string ConnectionString =
        "Host=127.0.0.1;Port=5434;Database=salesdeliverybi;Username=salesdeliverybi;Password=salesdeliverybi;SSL Mode=Disable";

    private readonly ApiWebApplicationFactory _factory;

    public QuotationsControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient(string? token)
    {
        HttpClient client = _factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static async Task<Guid> ScalarGuidAsync(string sql, string paramValue)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue(paramValue);
        object? result = await command.ExecuteScalarAsync();
        return result is Guid guid ? guid : throw new InvalidOperationException($"Query returned no row for '{paramValue}'.");
    }

    private static Task<Guid> GetUnitIdAsync(string unitName) =>
        ScalarGuidAsync("""SELECT "Id" FROM sales."Units" WHERE "UnitName" = $1""", unitName);

    private static Task<Guid> GetQuotationIdAsync(string quotationNo) =>
        ScalarGuidAsync("""SELECT "Id" FROM sales."Quotations" WHERE "QuotationNo" = $1""", quotationNo);

    [Fact]
    public async Task GetPipeline_NoToken_Returns401()
    {
        HttpClient client = CreateClient(null);

        HttpResponseMessage response = await client.GetAsync("/api/sales/quotations/pipeline");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPipeline_MissingQuotationViewPermission_Returns403()
    {
        string token = TestJwtTokenFactory.Create(Guid.NewGuid(), permissions: [], unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync("/api/sales/quotations/pipeline");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPipeline_ViewAllUnits_Returns200WithDataAndLastRefreshEnvelope()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync("/api/sales/quotations/pipeline");

        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("data", out JsonElement data));
        Assert.True(doc.RootElement.TryGetProperty("lastRefresh", out _));
        Assert.True(data.GetProperty("kpis").GetProperty("openQuotationsCount").GetInt32() >= 0);
    }

    [Fact]
    public async Task GetPipeline_RequestedUnitWithinAssignment_Returns200()
    {
        Guid unit1Id = await GetUnitIdAsync("Unit-1");
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView],
            unitIds: [unit1Id]);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync($"/api/sales/quotations/pipeline?unitId={unit1Id}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPipeline_ViewAllUnits_CanQueryAnyUnitEvenWithNoAssignment()
    {
        // caller has viewAllUnits and is assigned to NO units at all — the permission alone must be
        // enough to query any unit, proving unit assignment is irrelevant once viewAllUnits is granted.
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync($"/api/sales/quotations/pipeline?unitId={Guid.NewGuid()}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPipeline_RequestedUnitOutsideAssignment_Returns403ProblemDetailsWithTraceId()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView],
            unitIds: [Guid.NewGuid()]); // assigned to some other unit, not the one requested
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync($"/api/sales/quotations/pipeline?unitId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(403, doc.RootElement.GetProperty("status").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task GetById_UnknownQuotation_Returns404ProblemDetails()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync($"/api/sales/quotations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, doc.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetById_KnownQuotation_Returns200WithItemsAndStatusHistory()
    {
        Guid quotationId = await GetQuotationIdAsync("QTN-2026-0001");
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync($"/api/sales/quotations/{quotationId}");

        response.EnsureSuccessStatusCode();
        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement data = doc.RootElement.GetProperty("data");
        Assert.Equal("QTN-2026-0001", data.GetProperty("quotationNo").GetString());
        Assert.True(data.GetProperty("items").GetArrayLength() > 0);
        Assert.True(data.GetProperty("statusHistory").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetConversion_ViewAllUnits_Returns200()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response =
            await client.GetAsync("/api/sales/quotations/conversion?fromDate=2026-06-01&toDate=2026-08-31");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAging_ViewAllUnits_Returns200()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync("/api/sales/quotations/aging");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetSummary_ViewAllUnits_Returns200()
    {
        string token = TestJwtTokenFactory.Create(
            Guid.NewGuid(),
            permissions: [PermissionCodes.QuotationView, PermissionCodes.QuotationViewAllUnits],
            unitIds: []);
        HttpClient client = CreateClient(token);

        HttpResponseMessage response = await client.GetAsync("/api/sales/quotations/summary");

        response.EnsureSuccessStatusCode();
    }
}
