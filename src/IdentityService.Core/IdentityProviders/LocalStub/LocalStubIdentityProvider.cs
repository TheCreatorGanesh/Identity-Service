using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using IdentityService.Core.Enums;
using IdentityService.Core.Interfaces;
using IdentityService.Core.Models;

namespace IdentityService.Core.IdentityProviders.LocalStub;

/// <summary>
/// In-memory stub identity provider to validate config-based provider switching and enable zero-network local testing.
/// </summary>
public sealed class LocalStubIdentityProvider : IExternalIdentityProvider
{
    private readonly ConcurrentDictionary<string, string> _userStore = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<LocalStubIdentityProvider> _logger;

    public LocalStubIdentityProvider(ILogger<LocalStubIdentityProvider> logger)
    {
        _logger = logger;
        _userStore["test@rest.com"] = "Password123!";
        _userStore["e.rodriguez@academic.edu"] = "securePassword123";
        _userStore["erodriguez_staff"] = "securePassword123";
    }

    public Task<ExternalAuthResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[LocalStubIdentityProvider] Authenticating {Email}", email);

        if (_userStore.TryGetValue(email, out var storedPassword))
        {
            if (storedPassword == password)
            {
                return Task.FromResult(ExternalAuthResult.Success(email, $"stub-user-{email.ToLowerInvariant()}"));
            }
            return Task.FromResult(ExternalAuthResult.Failed(AuthFailureReason.InvalidCredentials, "Invalid credentials."));
        }

        if (password == "Password123!" || password == "securePassword123")
        {
            _userStore[email] = password;
            return Task.FromResult(ExternalAuthResult.Success(email, $"stub-user-{email.ToLowerInvariant()}"));
        }

        return Task.FromResult(ExternalAuthResult.Failed(AuthFailureReason.InvalidCredentials, "Invalid credentials."));
    }

    public Task<PasswordResetResult> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[LocalStubIdentityProvider] Password reset email triggered for {Email}", email);
        return Task.FromResult(PasswordResetResult.Success(email));
    }

    public Task<PasswordResetResult> ConfirmPasswordResetAsync(string resetCode, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[LocalStubIdentityProvider] Confirming password reset with code: {Code}", resetCode);

        if (string.IsNullOrWhiteSpace(resetCode) || resetCode == "invalid_code")
        {
            return Task.FromResult(PasswordResetResult.Failed(AuthFailureReason.InvalidOrExpiredResetCode, "Invalid reset code."));
        }

        if (newPassword.Length < 6)
        {
            return Task.FromResult(PasswordResetResult.Failed(AuthFailureReason.WeakPassword, "Password must be at least 6 characters."));
        }

        return Task.FromResult(PasswordResetResult.Success());
    }

    public Task<PasswordChangeResult> ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[LocalStubIdentityProvider] Changing password for {Email}", email);

        if (_userStore.TryGetValue(email, out var storedPassword) && storedPassword != currentPassword)
        {
            return Task.FromResult(PasswordChangeResult.Failed(AuthFailureReason.InvalidCredentials, "Current password is incorrect."));
        }

        if (newPassword.Length < 6)
        {
            return Task.FromResult(PasswordChangeResult.Failed(AuthFailureReason.WeakPassword, "New password is too weak."));
        }

        _userStore[email] = newPassword;
        return Task.FromResult(PasswordChangeResult.Success());
    }
}
