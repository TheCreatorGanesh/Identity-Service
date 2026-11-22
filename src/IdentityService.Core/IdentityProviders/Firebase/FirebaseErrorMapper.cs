using IdentityService.Core.Enums;

namespace IdentityService.Core.IdentityProviders.Firebase;

/// <summary>
/// Maps Firebase error tokens and codes to normalized AuthFailureReason enum values.
/// </summary>
public static class FirebaseErrorMapper
{
    public static AuthFailureReason MapErrorMessage(string? rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
            return AuthFailureReason.ProviderUnavailable;

        var code = rawMessage.Split(':', StringSplitOptions.TrimEntries)[0].Trim().ToUpperInvariant();

        return code switch
        {
            "INVALID_LOGIN_CREDENTIALS" => AuthFailureReason.InvalidCredentials,
            "EMAIL_NOT_FOUND" => AuthFailureReason.InvalidCredentials,
            "INVALID_PASSWORD" => AuthFailureReason.InvalidCredentials,
            "USER_DISABLED" => AuthFailureReason.UserDisabled,
            "TOO_MANY_ATTEMPTS_TRY_LATER" => AuthFailureReason.TooManyAttempts,
            "EXPIRED_OOB_CODE" => AuthFailureReason.InvalidOrExpiredResetCode,
            "INVALID_OOB_CODE" => AuthFailureReason.InvalidOrExpiredResetCode,
            "WEAK_PASSWORD" => AuthFailureReason.WeakPassword,
            "CREDENTIAL_TOO_OLD_LOGIN_AGAIN" => AuthFailureReason.RequiresRecentLogin,
            _ => AuthFailureReason.ProviderUnavailable
        };
    }
}
