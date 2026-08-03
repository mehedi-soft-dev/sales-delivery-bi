using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SalesDeliveryBI.Application.Common;

namespace SalesDeliveryBI.Api.Middleware;

/// <summary>
/// Maps exceptions to RFC 7807 Problem Details (api-contract.md). ForbiddenAccessException (row-level unit
/// scoping, security-plan.md §4) becomes 403 with its own client-safe message; everything else is an
/// unexpected 500 — the real exception is logged server-side but never handed to the client.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private const string _subClaimType = "sub";

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int statusCode, string title, string detail) = exception switch
        {
            ForbiddenAccessException forbidden => (StatusCodes.Status403Forbidden, "Forbidden", forbidden.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                "An unexpected error occurred. Contact support with the trace ID."),
        };

        // Never log names/emails/full JWTs — sub (caller's user id) is the one identifier safe to log.
        // Read directly off httpContext.User rather than injecting ICurrentUserContext (scoped) into this
        // singleton exception handler — that combination throws at startup under scope validation.
        string? userId = httpContext.User.FindFirst(_subClaimType)?.Value;

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Path} for {UserId}",
                httpContext.Request.Path, userId);
        }
        else if (statusCode == StatusCodes.Status403Forbidden)
        {
            _logger.LogInformation("Forbidden: {UserId} denied on {Path}: {Reason}",
                userId, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }
}
