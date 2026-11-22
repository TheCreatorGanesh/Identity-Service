using Refit;
using IdentityService.Core.DTOs;

namespace IdentityService.IntegrationTests.Clients;

public interface IAuthApiClient
{
    [Post("/api/v1/auth/login")]
    Task<ApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest request, CancellationToken cancellationToken = default);

    [Post("/api/v1/auth/logout")]
    Task<ApiResponse<object>> LogoutAsync(CancellationToken cancellationToken = default);

    [Post("/api/v1/auth/forgot-password")]
    Task<ApiResponse<ForgotPasswordResponse>> ForgotPasswordAsync([Body] ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    [Post("/api/v1/auth/reset-password")]
    Task<ApiResponse<ResetPasswordResponse>> ResetPasswordAsync([Body] ResetPasswordRequest request, CancellationToken cancellationToken = default);

    [Post("/api/v1/auth/change-password")]
    Task<ApiResponse<ChangePasswordResponse>> ChangePasswordAsync([Body] ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
