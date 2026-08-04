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

    /// <summary>
    /// The one Admin write path (discussed-and-scoped-in) — user/role CRUD and unit assignment remain out
    /// of scope. Gated by AdminWrite on top of the class-level AdminRead, so a caller needs both permission codes.
    /// </summary>
    [HttpPut("roles/{roleId:guid}/permissions")]
    [Authorize(Policy = AuthorizationPolicies.AdminWrite)]
    public async Task<ActionResult<AdminRoleDto>> UpdateRolePermissions(
        Guid roleId, [FromBody] UpdateRolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        AdminRoleDto? updated;
        try
        {
            updated = await _appService.UpdateRolePermissionsAsync(roleId, request.PermissionCodes, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return Problem(title: "Invalid permission codes", statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }

        if (updated is null)
        {
            return Problem(
                title: "Role not found",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No role '{roleId}' was found.");
        }

        return Ok(updated);
    }
}
