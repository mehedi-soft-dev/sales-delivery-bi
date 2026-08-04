using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;
using SalesDeliveryBI.Infrastructure.Security;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>View-only — no create/edit/delete actions exist for Users/Roles/Permissions (docs/plans/security/security-plan.md).</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminRead)]
public class AdminController : ControllerBase
{
    private readonly AdminAppService _appService;

    public AdminController(AdminAppService appService)
    {
        _appService = appService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers([FromQuery] GridQuery grid, CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetUsersAsync(grid, cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<PagedResult<AdminRoleDto>>> GetRoles([FromQuery] GridQuery grid, CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetRolesAsync(grid, cancellationToken));
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<PagedResult<AdminPermissionDto>>> GetPermissions([FromQuery] GridQuery grid, CancellationToken cancellationToken)
    {
        return Ok(await _appService.GetPermissionsAsync(grid, cancellationToken));
    }
}
