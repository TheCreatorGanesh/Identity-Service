using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityService.Application.Interfaces;
using IdentityService.Core.DTOs;
using IdentityService.Core.Enums;

namespace IdentityService.API.Controllers;

/// <summary>
/// Controller handling provider-agnostic user authentication, password resets, and session management.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IAuthService authService,
    ILogger<AuthController> logger
) : ControllerBase
{
    /// <summary>
    /// Authenticates credentials against the configured identity provider, retrieves internal user role, and mints a JWT.
    /// </summary>
    /// <param name="request">Authentication request containing email and password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Successful authentication, returning the JWT token and user profile.</response>
    /// <response code="401">Invalid credentials or user not authorized in the system.</response>
    /// <response code="403">User account is disabled.</response>
    /// <response code="429">Too many failed login attempts.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (success, data, failureReason, errorDetail) = await authService.LoginAsync(request, ipAddress, cancellationToken);

        if (success && data != null)
        {
            return Ok(data);
        }

        var errorResponse = new ErrorResponse
        {
            Error = failureReason.ToString(),
            Detail = errorDetail ?? "Authentication failed.",
            TraceId = HttpContext.TraceIdentifier
        };

        return failureReason switch
        {
            AuthFailureReason.UserDisabled => StatusCode(StatusCodes.Status403Forbidden, errorResponse),
            AuthFailureReason.TooManyAttempts => StatusCode(StatusCodes.Status429TooManyRequests, errorResponse),
            AuthFailureReason.ProviderUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, errorResponse),
            _ => Unauthorized(errorResponse)
        };
    }

    /// <summary>
    /// Dispatches a password reset email or verification link.
    /// </summary>
    /// <remarks>
    /// Always returns 200 OK with a generic message to prevent email and account enumeration attacks.
    /// </remarks>
    /// <param name="request">Forgot password payload containing the user's email.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Password reset process initiated.</response>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Completes the password reset process using an out-of-band verification reset code.
    /// </summary>
    /// <param name="request">Reset password payload containing resetCode and newPassword.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Password successfully reset.</response>
    /// <response code="400">Invalid or expired reset code, or weak password.</response>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var (success, data, failureReason, errorDetail) = await authService.ResetPasswordAsync(request, cancellationToken);

        if (success && data != null)
        {
            return Ok(data);
        }

        return BadRequest(new ErrorResponse
        {
            Error = failureReason.ToString(),
            Detail = errorDetail ?? "Invalid or expired reset code.",
            TraceId = HttpContext.TraceIdentifier
        });
    }

    /// <summary>
    /// Updates the password for the currently authenticated user.
    /// </summary>
    /// <remarks>
    /// Requires an authenticated Bearer token. Reads email strictly from the validated JWT claims.
    /// </remarks>
    /// <param name="request">Change password payload with current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Password successfully updated.</response>
    /// <response code="400">Invalid password parameters or incorrect current password.</response>
    /// <response code="401">Unauthorized / Missing token.</response>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
                 ?? User.FindFirstValue("email")
                 ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Change password attempted without valid email claim in token.");
            return Unauthorized(new ErrorResponse
            {
                Error = "Unauthorized",
                Detail = "Authenticated user email claim could not be resolved from JWT.",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var (success, data, failureReason, errorDetail) = await authService.ChangePasswordAsync(email, request, cancellationToken);

        if (success && data != null)
        {
            return Ok(data);
        }

        return failureReason switch
        {
            AuthFailureReason.InvalidCredentials => BadRequest(new ErrorResponse
            {
                Error = "InvalidCredentials",
                Detail = errorDetail ?? "Current password is incorrect.",
                TraceId = HttpContext.TraceIdentifier
            }),
            AuthFailureReason.RequiresRecentLogin => Unauthorized(new ErrorResponse
            {
                Error = "RequiresRecentLogin",
                Detail = "This sensitive operation requires a recent login. Please log in again.",
                TraceId = HttpContext.TraceIdentifier
            }),
            _ => BadRequest(new ErrorResponse
            {
                Error = failureReason.ToString(),
                Detail = errorDetail ?? "Failed to change password.",
                TraceId = HttpContext.TraceIdentifier
            })
        };
    }

    /// <summary>
    /// Logs out the user and invalidates the client session.
    /// </summary>
    /// <response code="200">Successfully logged out.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        return Ok(new { Message = "Logged out successfully" });
    }
}
