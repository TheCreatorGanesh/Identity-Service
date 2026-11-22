using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IdentityService.Core.DTOs;

/// <summary>
/// Data transfer object representing an authentication request.
/// </summary>
public sealed class LoginRequest
{
    private string _email = string.Empty;

    /// <summary>
    /// User email address or staff identifier.
    /// </summary>
    /// <example>test@rest.com</example>
    [Required]
    public required string Email
    {
        get => _email;
        set => _email = value;
    }

    /// <summary>
    /// Alias for backwards compatibility with existing UI (which sends staffId).
    /// </summary>
    [JsonPropertyName("staffId")]
    public string? StaffId
    {
        get => _email;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_email))
            {
                _email = value;
            }
        }
    }

    /// <summary>
    /// Account password.
    /// </summary>
    /// <example>Password123!</example>
    [Required]
    public required string Password { get; set; }

    /// <summary>
    /// Indicates whether to extend token lifetime (e.g. 7 days vs 1 hour).
    /// </summary>
    /// <example>false</example>
    public bool RememberMe { get; set; }
}

/// <summary>
/// Data transfer object returned upon successful authentication.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// The minted ASP.NET Core JWT bearer token.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Token lifetime in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Profile information for the authenticated user.
    /// </summary>
    public required UserProfile User { get; set; }
}

/// <summary>
/// Authenticated user profile information.
/// </summary>
public sealed class UserProfile
{
    public required string EmployeeId { get; set; }
    public required string Username { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string Phone { get; set; }
    public required string Dept { get; set; }
    public string? LastPasswordReset { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public string? LastLoginIp { get; set; }
    public bool MustResetPassword { get; set; }
}

/// <summary>
/// Request to initiate a password reset.
/// </summary>
public sealed class ForgotPasswordRequest
{
    private string _email = string.Empty;

    /// <summary>
    /// The user's registered email address.
    /// </summary>
    /// <example>test@rest.com</example>
    [Required]
    public required string Email
    {
        get => _email;
        set => _email = value;
    }

    /// <summary>
    /// Alias for backwards compatibility with UI (which sends emailOrStaffId).
    /// </summary>
    [JsonPropertyName("emailOrStaffId")]
    public string? EmailOrStaffId
    {
        get => _email;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_email))
            {
                _email = value;
            }
        }
    }
}

/// <summary>
/// Response returned upon requesting a password reset.
/// </summary>
public sealed class ForgotPasswordResponse
{
    public required string Message { get; set; }
}

/// <summary>
/// Request to confirm a password reset.
/// </summary>
public sealed class ResetPasswordRequest
{
    private string _resetCode = string.Empty;

    /// <summary>
    /// The verification reset code / out-of-band token.
    /// </summary>
    [Required]
    public required string ResetCode
    {
        get => _resetCode;
        set => _resetCode = value;
    }

    /// <summary>
    /// Alias for backwards compatibility with UI (which sends token).
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token
    {
        get => _resetCode;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(_resetCode))
            {
                _resetCode = value;
            }
        }
    }

    /// <summary>
    /// The new password to set.
    /// </summary>
    [Required]
    [MinLength(6)]
    public required string NewPassword { get; set; }

    /// <summary>
    /// Optional email or staff identifier for auditing.
    /// </summary>
    public string? EmailOrStaffId { get; set; }
}

/// <summary>
/// Response returned upon successful password reset.
/// </summary>
public sealed class ResetPasswordResponse
{
    public required string Message { get; set; }
}

/// <summary>
/// Request to change the account password while authenticated.
/// </summary>
public sealed class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; set; }

    [Required]
    [MinLength(6)]
    public required string NewPassword { get; set; }
}

/// <summary>
/// Response returned upon successful password change.
/// </summary>
public sealed class ChangePasswordResponse
{
    public required string Message { get; set; }
}
