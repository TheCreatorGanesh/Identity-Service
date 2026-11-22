using IdentityService.Core.Enums;

namespace IdentityService.Core.Models;

/// <summary>
/// Provider-agnostic result of a password change operation.
/// </summary>
public sealed record PasswordChangeResult
{
    public bool IsSuccess { get; init; }
    public AuthFailureReason FailureReason { get; init; } = AuthFailureReason.None;
    public string? ErrorMessage { get; init; }

    public static PasswordChangeResult Success() =>
        new()
        {
            IsSuccess = true,
            FailureReason = AuthFailureReason.None
        };

    public static PasswordChangeResult Failed(AuthFailureReason reason, string? errorMessage = null) =>
        new()
        {
            IsSuccess = false,
            FailureReason = reason,
            ErrorMessage = errorMessage
        };
}
