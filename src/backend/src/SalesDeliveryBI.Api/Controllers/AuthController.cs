using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Dtos;
using SalesDeliveryBI.Application.Services;

namespace SalesDeliveryBI.Api.Controllers;

/// <summary>Anonymous by default (unlike QuotationsController) — no [Authorize], this is the login entry point.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthAppService _appService;

    public AuthController(AuthAppService appService)
    {
        _appService = appService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        LoginResponseDto? response = await _appService.LoginAsync(request, cancellationToken);

        if (response is null)
        {
            // Never reveal whether the email exists — one generic message for wrong-password, unknown-email,
            // and inactive-user (security-plan.md's anti-enumeration instinct, same as the 404-vs-403 case).
            return Problem(
                title: "Invalid email or password",
                statusCode: StatusCodes.Status401Unauthorized,
                detail: "Check your credentials and try again.");
        }

        return Ok(response);
    }
}
