using IdentityService.Core.Enums;

namespace IdentityService.Core.Models;

/// <summary>
/// Provider-agnostic result of a password reset request or confirmation.
/// </summary>
public sealed record PasswordResetResult
{
    public bool IsSuccess { get; init; }
    public string? Email { get; init; }
    public AuthFailureReason FailureReason { get; init; } = AuthFailureReason.None;
    public string? ErrorMessage { get; init; }

    public static PasswordResetResult Success(string? email = null) =>
        new()
        {
            IsSuccess = true,
            Email = email,
            FailureReason = AuthFailureReason.None
        };

    public static PasswordResetResult Failed(AuthFailureReason reason, string? errorMessage = null) =>
        new()
        {
            IsSuccess = false,
            FailureReason = reason,
            ErrorMessage = errorMessage
        };
}
