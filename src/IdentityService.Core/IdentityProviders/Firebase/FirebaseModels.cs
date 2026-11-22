using System.Text.Json.Serialization;

namespace IdentityService.Core.IdentityProviders.Firebase;

internal sealed class FirebaseSignInRequest
{
    [JsonPropertyName("email")]
    public required string Email { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("returnSecureToken")]
    public bool ReturnSecureToken { get; set; } = true;
}

internal sealed class FirebaseSignInResponse
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public string? ExpiresIn { get; set; }

    [JsonPropertyName("localId")]
    public string? LocalId { get; set; }
}

internal sealed class FirebaseSendOobCodeRequest
{
    [JsonPropertyName("requestType")]
    public string RequestType { get; set; } = "PASSWORD_RESET";

    [JsonPropertyName("email")]
    public required string Email { get; set; }
}

internal sealed class FirebaseSendOobCodeResponse
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal sealed class FirebaseResetPasswordRequest
{
    [JsonPropertyName("oobCode")]
    public required string OobCode { get; set; }

    [JsonPropertyName("newPassword")]
    public required string NewPassword { get; set; }
}

internal sealed class FirebaseResetPasswordResponse
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("requestType")]
    public string? RequestType { get; set; }
}

internal sealed class FirebaseUpdatePasswordRequest
{
    [JsonPropertyName("idToken")]
    public required string IdToken { get; set; }

    [JsonPropertyName("password")]
    public required string Password { get; set; }

    [JsonPropertyName("returnSecureToken")]
    public bool ReturnSecureToken { get; set; } = false;
}

internal sealed class FirebaseUpdatePasswordResponse
{
    [JsonPropertyName("localId")]
    public string? LocalId { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

internal sealed class FirebaseErrorContainer
{
    [JsonPropertyName("error")]
    public FirebaseErrorDetail? Error { get; set; }
}

internal sealed class FirebaseErrorDetail
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public List<FirebaseSubError>? Errors { get; set; }
}

internal sealed class FirebaseSubError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
