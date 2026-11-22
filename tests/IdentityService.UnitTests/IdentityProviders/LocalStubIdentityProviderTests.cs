using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using IdentityService.Core.Enums;
using IdentityService.Core.IdentityProviders.LocalStub;
using Xunit;

namespace IdentityService.UnitTests.IdentityProviders;

public sealed class LocalStubIdentityProviderTests
{
    [Fact]
    public async Task AuthenticateAsync_WithSeededCredentials_ReturnsSuccess()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.AuthenticateAsync("test@rest.com", "Password123!");

        result.IsSuccess.Should().BeTrue();
        result.Email.Should().Be("test@rest.com");
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsFailed()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.AuthenticateAsync("test@rest.com", "WrongPassword");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(AuthFailureReason.InvalidCredentials);
    }

    [Fact]
    public async Task SendPasswordResetAsync_ReturnsSuccess()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.SendPasswordResetAsync("anyone@example.com");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WithValidCodeAndStrongPassword_ReturnsSuccess()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.ConfirmPasswordResetAsync("any-reset-code", "newPassword123!");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WithShortPassword_ReturnsWeakPassword()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.ConfirmPasswordResetAsync("any-reset-code", "123");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(AuthFailureReason.WeakPassword);
    }

    [Fact]
    public async Task ChangePasswordAsync_UpdatesPasswordSuccessfully()
    {
        var stub = new LocalStubIdentityProvider(NullLogger<LocalStubIdentityProvider>.Instance);

        var result = await stub.ChangePasswordAsync("test@rest.com", "Password123!", "NewSecret123!");
        result.IsSuccess.Should().BeTrue();

        var reauth = await stub.AuthenticateAsync("test@rest.com", "NewSecret123!");
        reauth.IsSuccess.Should().BeTrue();
    }
}
