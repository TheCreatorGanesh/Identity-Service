using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IdentityService.Core.Configuration;
using IdentityService.Core.Enums;
using IdentityService.Core.Interfaces;
using IdentityService.Core.Models;

namespace IdentityService.Core.IdentityProviders.Firebase;

/// <summary>
/// Implementation of IExternalIdentityProvider using Firebase Auth REST API (Google Identity Toolkit v1).
/// </summary>
public sealed class FirebaseIdentityProvider : IExternalIdentityProvider
{
    private readonly HttpClient _httpClient;
    private readonly FirebaseOptions _firebaseOptions;
    private readonly ILogger<FirebaseIdentityProvider> _logger;

    public FirebaseIdentityProvider(
        HttpClient httpClient,
        IOptions<FirebaseOptions> firebaseOptions,
        ILogger<FirebaseIdentityProvider> logger)
    {
        _httpClient = httpClient;
        _firebaseOptions = firebaseOptions.Value;
        _logger = logger;
    }

    public async Task<ExternalAuthResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Authenticating user with Firebase: {Email}", email);

        if (_firebaseOptions.BypassSignatureValidation)
        {
            _logger.LogInformation("Bypassing Firebase signature validation (development/mock mode) for {Email}", email);
            return ExternalAuthResult.Success(email, $"mock-sub-{email.GetHashCode():X}");
        }

        try
        {
            var url = _firebaseOptions.GetSignInUrl();
            var payload = new FirebaseSignInRequest
            {
                Email = email,
                Password = password,
                ReturnSecureToken = true
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
                return ExternalAuthResult.Success(data?.Email ?? email, data?.LocalId);
            }

            var failureReason = await ParseFailureReasonAsync(response, cancellationToken);
            _logger.LogWarning("Firebase authentication failed for {Email}: {Reason}", email, failureReason);
            return ExternalAuthResult.Failed(failureReason, "Invalid credentials or login failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Firebase authentication for {Email}", email);
            return ExternalAuthResult.Failed(AuthFailureReason.ProviderUnavailable, "Identity provider is currently unreachable.");
        }
    }

    public async Task<PasswordResetResult> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending password reset email via Firebase for: {Email}", email);

        if (_firebaseOptions.BypassSignatureValidation)
        {
            _logger.LogInformation("Bypassing Firebase password reset email dispatch (development/mock mode) for {Email}", email);
            return PasswordResetResult.Success(email);
        }

        try
        {
            var url = _firebaseOptions.GetSendOobCodeUrl();
            var payload = new FirebaseSendOobCodeRequest
            {
                RequestType = "PASSWORD_RESET",
                Email = email
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return PasswordResetResult.Success(email);
            }

            var rawMessage = await ExtractFirebaseErrorMessageAsync(response, cancellationToken);
            var failureReason = FirebaseErrorMapper.MapErrorMessage(rawMessage);

            if (failureReason == AuthFailureReason.InvalidCredentials || rawMessage?.Contains("EMAIL_NOT_FOUND", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation("Firebase returned EMAIL_NOT_FOUND for {Email}. Suppressing error to prevent enumeration.", email);
                return PasswordResetResult.Success(email);
            }

            _logger.LogWarning("Firebase send password reset failed for {Email}: {Reason}", email, failureReason);
            return PasswordResetResult.Failed(failureReason, rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Firebase password reset for {Email}", email);
            return PasswordResetResult.Failed(AuthFailureReason.ProviderUnavailable, "Identity provider is currently unreachable.");
        }
    }

    public async Task<PasswordResetResult> ConfirmPasswordResetAsync(string resetCode, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Confirming password reset with Firebase using verification code");

        if (_firebaseOptions.BypassSignatureValidation)
        {
            _logger.LogInformation("Bypassing Firebase reset password confirmation (development/mock mode)");
            return PasswordResetResult.Success();
        }

        try
        {
            var url = _firebaseOptions.GetResetPasswordUrl();
            var payload = new FirebaseResetPasswordRequest
            {
                OobCode = resetCode,
                NewPassword = newPassword
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<FirebaseResetPasswordResponse>(cancellationToken: cancellationToken);
                return PasswordResetResult.Success(data?.Email);
            }

            var rawMessage = await ExtractFirebaseErrorMessageAsync(response, cancellationToken);
            var failureReason = FirebaseErrorMapper.MapErrorMessage(rawMessage);
            _logger.LogWarning("Firebase confirm password reset failed: {Reason}", failureReason);
            return PasswordResetResult.Failed(failureReason, rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Firebase confirm password reset");
            return PasswordResetResult.Failed(AuthFailureReason.ProviderUnavailable, "Identity provider is currently unreachable.");
        }
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(string email, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating authenticated password change for {Email}", email);

        if (_firebaseOptions.BypassSignatureValidation)
        {
            _logger.LogInformation("Bypassing Firebase password change (development/mock mode) for {Email}", email);
            return PasswordChangeResult.Success();
        }

        try
        {
            var signInUrl = _firebaseOptions.GetSignInUrl();
            var signInPayload = new FirebaseSignInRequest
            {
                Email = email,
                Password = currentPassword,
                ReturnSecureToken = true
            };

            var signInResponse = await _httpClient.PostAsJsonAsync(signInUrl, signInPayload, cancellationToken);
            if (!signInResponse.IsSuccessStatusCode)
            {
                var signInError = await ExtractFirebaseErrorMessageAsync(signInResponse, cancellationToken);
                var reason = FirebaseErrorMapper.MapErrorMessage(signInError);
                _logger.LogWarning("Current password verification failed for {Email}: {Reason}", email, reason);
                return PasswordChangeResult.Failed(AuthFailureReason.InvalidCredentials, "Current password is incorrect.");
            }

            var signInData = await signInResponse.Content.ReadFromJsonAsync<FirebaseSignInResponse>(cancellationToken: cancellationToken);
            var freshIdToken = signInData?.IdToken;
            if (string.IsNullOrWhiteSpace(freshIdToken))
            {
                return PasswordChangeResult.Failed(AuthFailureReason.ProviderUnavailable, "Failed to retrieve identity verification token.");
            }

            var updateUrl = _firebaseOptions.GetUpdateUrl();
            var updatePayload = new FirebaseUpdatePasswordRequest
            {
                IdToken = freshIdToken,
                Password = newPassword,
                ReturnSecureToken = false
            };

            var updateResponse = await _httpClient.PostAsJsonAsync(updateUrl, updatePayload, cancellationToken);
            if (updateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Firebase password successfully changed for {Email}", email);
                return PasswordChangeResult.Success();
            }

            var updateError = await ExtractFirebaseErrorMessageAsync(updateResponse, cancellationToken);
            var updateReason = FirebaseErrorMapper.MapErrorMessage(updateError);
            _logger.LogWarning("Password update failed in Firebase for {Email}: {Reason}", email, updateReason);
            return PasswordChangeResult.Failed(updateReason, updateError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception during Firebase password change for {Email}", email);
            return PasswordChangeResult.Failed(AuthFailureReason.ProviderUnavailable, "Identity provider is currently unreachable.");
        }
    }

    private static async Task<AuthFailureReason> ParseFailureReasonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var rawMessage = await ExtractFirebaseErrorMessageAsync(response, ct);
        return FirebaseErrorMapper.MapErrorMessage(rawMessage);
    }

    private static async Task<string?> ExtractFirebaseErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var errorObj = JsonSerializer.Deserialize<FirebaseErrorContainer>(content);
            return errorObj?.Error?.Message;
        }
        catch
        {
            return null;
        }
    }
}
