using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Thin controller per docs/plans/backend/architecture.md — calls QuotationAppService directly, no indirection.</summary>
[ApiController]
[Route("api/sales/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly QuotationAppService _appService;

    public QuotationsController(QuotationAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("pipeline")]
    [Authorize(Policy = AuthorizationPolicies.QuotationPipelineRead)]
    public async Task<ActionResult<DashboardResponse<QuotationPipelineResponseDto>>> GetPipeline(
        [FromQuery] Guid? unitId,
        [FromQuery] bool includeDraft,
        [FromQuery] string? status,
        [FromQuery] string? buyerName,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetPipelineAsync(unitId, includeDraft, status, buyerName, fromDate, toDate, grid, cancellationToken));
    }

    /// <summary>Excel export of the (optionally status/buyer-filtered) Pipeline grid — same policy/guard as the dashboard itself.</summary>
    [HttpGet("pipeline/export")]
    [Authorize(Policy = AuthorizationPolicies.QuotationPipelineRead)]
    public async Task<IActionResult> ExportPipeline(
        [FromQuery] Guid? unitId,
        [FromQuery] bool includeDraft,
        [FromQuery] string? status,
        [FromQuery] string? buyerName,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        (byte[] content, DateTime lastRefresh) =
            await _appService.ExportPipelineAsync(unitId, includeDraft, status, buyerName, fromDate, toDate, cancellationToken);

        string fileName = $"quotation-pipeline-{lastRefresh:yyyyMMdd-HHmm}.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("conversion")]
    [Authorize(Policy = AuthorizationPolicies.QuotationConversionRead)]
    public async Task<ActionResult<DashboardResponse<ConversionResponseDto>>> GetConversion(
        [FromQuery] Guid? unitId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly effectiveFromDate = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        DateOnly effectiveToDate = toDate ?? today;

        return Ok(await _appService.GetConversionAsync(unitId, effectiveFromDate, effectiveToDate, grid, cancellationToken));
    }

    [HttpGet("aging")]
    [Authorize(Policy = AuthorizationPolicies.QuotationAgingRead)]
    public async Task<ActionResult<DashboardResponse<AgingResponseDto>>> GetAging(
        [FromQuery] Guid? unitId,
        [FromQuery] bool includeDraft,
        [FromQuery] bool highRiskOnly,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetAgingAsync(unitId, includeDraft, highRiskOnly, fromDate, toDate, grid, cancellationToken));
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.QuotationSummaryRead)]
    public async Task<ActionResult<DashboardResponse<QuotationSummaryDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, cancellationToken));
    }

    /// <summary>Units the caller may filter dashboards by — backs the topbar's unit dropdown.</summary>
    [HttpGet("units")]
    [Authorize(Policy = AuthorizationPolicies.QuotationViewAny)]
    public async Task<ActionResult<IReadOnlyList<UnitOptionDto>>> GetUnits(CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetUnitsAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.QuotationViewAny)]
    public async Task<ActionResult<DashboardResponse<QuotationDetailDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        DashboardResponse<QuotationDetailDto?> response = await _appService.GetByIdAsync(id, cancellationToken);

        if (response.Data is null)
        {
            // Out-of-scope and genuinely-missing quotations both land here — never 403, so a caller can't
            // tell the difference between "doesn't exist" and "exists but isn't yours" (security-plan.md).
            return Problem(
                title: "Quotation not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No quotation '{id}' was found for the caller's assigned units.");
        }

        return Ok(new DashboardResponse<QuotationDetailDto>(response.Data, response.LastRefresh));
    }
}
