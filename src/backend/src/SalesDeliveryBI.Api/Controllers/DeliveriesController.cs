using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Thin controller per docs/plans/backend/architecture.md — calls DeliveryAppService directly, no indirection.</summary>
[ApiController]
[Route("api/sales/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly DeliveryAppService _appService;

    public DeliveriesController(DeliveryAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.DeliveryRead)]
    public async Task<ActionResult<DashboardResponse<DeliveryResponseDto>>> GetSummary(
        [FromQuery] Guid? unitId,
        [FromQuery] GridQuery grid,
        CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetSummaryAsync(unitId, grid, cancellationToken));
    }
}
