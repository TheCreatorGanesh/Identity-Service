using IdentityService.Core.Enums;

namespace IdentityService.Core.Models;

/// <summary>
/// Provider-agnostic result of an authentication operation.
/// </summary>
public sealed record ExternalAuthResult
{
    public bool IsSuccess { get; init; }
    public string? Email { get; init; }
    public string? ProviderUserId { get; init; }
    public AuthFailureReason FailureReason { get; init; } = AuthFailureReason.None;
    public string? ErrorMessage { get; init; }

    public static ExternalAuthResult Success(string email, string? providerUserId = null) =>
        new()
        {
            IsSuccess = true,
            Email = email,
            ProviderUserId = providerUserId,
            FailureReason = AuthFailureReason.None
        };

    public static ExternalAuthResult Failed(AuthFailureReason reason, string? errorMessage = null) =>
        new()
        {
            IsSuccess = false,
            FailureReason = reason,
            ErrorMessage = errorMessage
        };
}
