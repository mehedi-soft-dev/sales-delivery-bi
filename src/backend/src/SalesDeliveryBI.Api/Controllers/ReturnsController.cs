using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Thin controller per docs/plans/backend/architecture.md — calls ReturnAppService directly, no indirection.</summary>
[ApiController]
[Route("api/sales/returns")]
[Authorize]
public class ReturnsController : ControllerBase
{
    private readonly ReturnAppService _appService;

    public ReturnsController(ReturnAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.ReturnRead)]
    public async Task<ActionResult<DashboardResponse<ReturnResponseDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, grid, cancellationToken));
    }
}
