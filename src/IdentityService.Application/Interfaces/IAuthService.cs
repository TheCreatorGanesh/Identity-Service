using IdentityService.Core.DTOs;
using IdentityService.Core.Enums;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Application service orchestrating authentication, Identity DB role retrieval, and password management.
/// </summary>
public interface IAuthService
{
    Task<(bool Success, LoginResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> LoginAsync(
        LoginRequest request, 
        string? ipAddress, 
        CancellationToken cancellationToken = default);

    Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request, 
        CancellationToken cancellationToken = default);

    Task<(bool Success, ResetPasswordResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> ResetPasswordAsync(
        ResetPasswordRequest request, 
        CancellationToken cancellationToken = default);

    Task<(bool Success, ChangePasswordResponse? Data, AuthFailureReason FailureReason, string? ErrorDetail)> ChangePasswordAsync(
        string email, 
        ChangePasswordRequest request, 
        CancellationToken cancellationToken = default);
}
