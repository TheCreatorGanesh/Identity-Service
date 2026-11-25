using Microsoft.Extensions.Logging;
using IdentityService.Application.Interfaces;
using IdentityService.Core.DTOs;
using IdentityService.Core.Enums;
using IdentityService.Core.Interfaces;
using IdentityService.Core.Models;

namespace IdentityService.Application.Services;

/// <summary>
/// Core authentication orchestrator. Strictly depends only on IExternalIdentityProvider,
/// IIdentityUserRepository, and IJwtTokenGenerator with no external SDK dependencies.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IExternalIdentityProvider _externalIdp;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IExternalIdentityProvider externalIdp,
        IIdentityUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _externalIdp = externalIdp;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<(bool Success, LoginResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing authentication request for: {Email}", request.Email);

        // Step 1: Authenticate credentials against configured identity provider
        var authResult = await _externalIdp.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (!authResult.IsSuccess)
        {
            _logger.LogWarning("Authentication failed for {Email}: {Reason}", request.Email, authResult.FailureReason);
            return (false, null, authResult.FailureReason, authResult.ErrorMessage ?? "Invalid email or password.");
        }

        // Step 2: Query Identity DB by normalized email
        var normalizedEmail = (authResult.Email ?? request.Email).ToLowerInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {Email} authenticated via IdP but does not exist in Identity DB.", normalizedEmail);
            return (false, null, AuthFailureReason.InvalidCredentials, "User account is not registered in the system.");
        }

        // Step 3: Mint ASP.NET Core JWT containing sub, email, ClaimTypes.Role, and userrole claims
        var (tokenString, expiresInSeconds) = _jwtTokenGenerator.GenerateToken(user, request.RememberMe);

        var loginTime = DateTimeOffset.UtcNow;
        _ = _userRepository.RecordLoginAsync(user.AggregateId, loginTime, ipAddress, cancellationToken);

        var response = new LoginResponse
        {
            Token = tokenString,
            ExpiresIn = expiresInSeconds,
            User = new UserProfile
            {
                Username = user.Username,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                LastPasswordReset = user.LastPasswordReset,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLogin = loginTime,
                LastLoginIp = ipAddress
            }
        };

        _logger.LogInformation("Successfully authenticated user {Username} ({AggregateId})", user.Username, user.AggregateId);
        return (true, response, AuthFailureReason.None, null);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating forgot-password dispatch for {Email}", request.Email);

        try
        {
            await _externalIdp.SendPasswordResetAsync(request.Email, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error dispatching password reset for {Email}", request.Email);
        }

        // Always return generic 200 OK response to prevent email enumeration
        return new ForgotPasswordResponse
        {
            Message = "If an account with that email exists, a password reset link has been dispatched."
        };
    }

    public async Task<(bool Success, ResetPasswordResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing reset password confirmation");

        var resetResult = await _externalIdp.ConfirmPasswordResetAsync(request.ResetCode, request.NewPassword, cancellationToken);
        if (!resetResult.IsSuccess)
        {
            _logger.LogWarning("Reset password confirmation failed: {Reason}", resetResult.FailureReason);
            return (false, null, resetResult.FailureReason, resetResult.ErrorMessage ?? "Invalid or expired password reset code.");
        }

        // If email or staff id is known, update password reset audit timestamp
        var identifier = resetResult.Email ?? request.EmailOrStaffId;
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            var user = await _userRepository.GetByNormalizedEmailAsync(identifier.ToLowerInvariant(), cancellationToken);
            if (user != null)
            {
                var today = DateTimeOffset.UtcNow.ToString("MMM dd, yyyy");
                await _userRepository.UpdatePasswordResetTimestampAsync(user.AggregateId, today, cancellationToken);
            }
        }

        return (true, new ResetPasswordResponse
        {
            Message = "Password has been successfully reset. You may now log in with your new password."
        }, AuthFailureReason.None, null);
    }

    public async Task<(bool Success, ChangePasswordResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> ChangePasswordAsync(
        string email,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing authenticated password change for {Email}", email);

        var changeResult = await _externalIdp.ChangePasswordAsync(email, request.CurrentPassword, request.NewPassword, cancellationToken);
        if (!changeResult.IsSuccess)
        {
            _logger.LogWarning("Password change failed for {Email}: {Reason}", email, changeResult.FailureReason);
            return (false, null, changeResult.FailureReason, changeResult.ErrorMessage ?? "Failed to change password.");
        }

        var user = await _userRepository.GetByNormalizedEmailAsync(email.ToLowerInvariant(), cancellationToken);
        if (user != null)
        {
            var today = DateTimeOffset.UtcNow.ToString("MMM dd, yyyy");
            await _userRepository.UpdatePasswordResetTimestampAsync(user.AggregateId, today, cancellationToken);
        }

        return (true, new ChangePasswordResponse
        {
            Message = "Password changed successfully."
        }, AuthFailureReason.None, null);
    }
}
