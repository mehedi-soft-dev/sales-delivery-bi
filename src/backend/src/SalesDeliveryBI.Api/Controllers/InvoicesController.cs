using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Thin controller per docs/plans/backend/architecture.md — calls InvoiceAppService directly, no indirection.</summary>
[ApiController]
[Route("api/sales/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceAppService _appService;

    public InvoicesController(InvoiceAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.InvoiceRead)]
    public async Task<ActionResult<DashboardResponse<InvoiceResponseDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, grid, cancellationToken));
    }
}
