using FluentAssertions;
using IdentityService.Core.Enums;
using IdentityService.Core.IdentityProviders.Firebase;
using Xunit;

namespace IdentityService.UnitTests.IdentityProviders;

public sealed class FirebaseErrorMapperTests
{
    [Theory]
    [InlineData("INVALID_LOGIN_CREDENTIALS", AuthFailureReason.InvalidCredentials)]
    [InlineData("EMAIL_NOT_FOUND", AuthFailureReason.InvalidCredentials)]
    [InlineData("INVALID_PASSWORD", AuthFailureReason.InvalidCredentials)]
    [InlineData("USER_DISABLED", AuthFailureReason.UserDisabled)]
    [InlineData("TOO_MANY_ATTEMPTS_TRY_LATER", AuthFailureReason.TooManyAttempts)]
    [InlineData("EXPIRED_OOB_CODE", AuthFailureReason.InvalidOrExpiredResetCode)]
    [InlineData("INVALID_OOB_CODE", AuthFailureReason.InvalidOrExpiredResetCode)]
    [InlineData("WEAK_PASSWORD : Password should be at least 6 characters", AuthFailureReason.WeakPassword)]
    [InlineData("CREDENTIAL_TOO_OLD_LOGIN_AGAIN", AuthFailureReason.RequiresRecentLogin)]
    [InlineData("UNKNOWN_FIREBASE_ERROR_CODE", AuthFailureReason.ProviderUnavailable)]
    [InlineData("", AuthFailureReason.ProviderUnavailable)]
    [InlineData(null, AuthFailureReason.ProviderUnavailable)]
    public void MapErrorMessage_ReturnsCorrectAuthFailureReason(string? rawError, AuthFailureReason expectedReason)
    {
        var result = FirebaseErrorMapper.MapErrorMessage(rawError);
        result.Should().Be(expectedReason);
    }
}
