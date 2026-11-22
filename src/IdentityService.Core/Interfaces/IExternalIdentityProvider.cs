using IdentityService.Core.Models;

namespace IdentityService.Core.Interfaces;

/// <summary>
/// Provider-agnostic abstraction for external identity management (e.g. Firebase, Auth0, Okta, LocalStub).
/// Business logic and controllers depend strictly on this abstraction.
/// </summary>
public interface IExternalIdentityProvider
{
    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// </summary>
    Task<ExternalAuthResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a password reset email/link dispatch for the specified email.
    /// </summary>
    Task<PasswordResetResult> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes a password reset using an out-of-band verification code and new password.
    /// </summary>
    Task<PasswordResetResult> ConfirmPasswordResetAsync(string resetCode, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the user's password after verifying their current password.
    /// </summary>
    Task<PasswordChangeResult> ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
