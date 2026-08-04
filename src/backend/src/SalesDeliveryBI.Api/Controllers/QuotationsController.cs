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
[Authorize(Policy = AuthorizationPolicies.QuotationRead)]
public class QuotationsController : ControllerBase
{
    private readonly QuotationAppService _appService;

    public QuotationsController(QuotationAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("pipeline")]
    public async Task<ActionResult<DashboardResponse<QuotationPipelineResponseDto>>> GetPipeline(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetPipelineAsync(unitId, grid, cancellationToken));
    }

    [HttpGet("conversion")]
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
    public async Task<ActionResult<DashboardResponse<AgingResponseDto>>> GetAging(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetAgingAsync(unitId, grid, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardResponse<QuotationSummaryDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
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
