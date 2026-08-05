using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Thin controller per docs/plans/backend/architecture.md — calls SalesOrderAppService directly, no indirection.</summary>
[ApiController]
[Route("api/sales/orders")]
[Authorize]
public class SalesOrdersController : ControllerBase
{
    private readonly SalesOrderAppService _appService;

    public SalesOrdersController(SalesOrderAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.SalesOrderRead)]
    public async Task<ActionResult<DashboardResponse<SalesOrderResponseDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, grid, cancellationToken));
    }
}
