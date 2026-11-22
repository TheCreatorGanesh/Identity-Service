namespace IdentityService.Core.Enums;

/// <summary>
/// Normalized provider-agnostic failure reasons for authentication operations.
/// </summary>
public enum AuthFailureReason
{
    None = 0,
    InvalidCredentials,
    UserDisabled,
    TooManyAttempts,
    InvalidOrExpiredResetCode,
    WeakPassword,
    RequiresRecentLogin,
    ProviderUnavailable
}
