using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;
using IdentityService.Core.DTOs;
using IdentityService.Core.Enums;
using IdentityService.Core.Interfaces;
using IdentityService.Core.Models;
using Xunit;

namespace IdentityService.UnitTests.Services;

public sealed class AuthServiceTests
{
    private readonly IExternalIdentityProvider _externalIdp = Substitute.For<IExternalIdentityProvider>();
    private readonly IIdentityUserRepository _userRepo = Substitute.For<IIdentityUserRepository>();
    private readonly IJwtTokenGenerator _jwtGen = Substitute.For<IJwtTokenGenerator>();

    private AuthService CreateSut() =>
        new(_externalIdp, _userRepo, _jwtGen, NullLogger<AuthService>.Instance);

    [Fact]
    public async Task LoginAsync_WhenCredentialsValidAndUserExists_ReturnsSuccessAndMintsToken()
    {
        var sut = CreateSut();
        var email = "dr.elena@academic.edu";
        var password = "SecurePassword123!";

        _externalIdp.AuthenticateAsync(email, password, Arg.Any<CancellationToken>())
            .Returns(ExternalAuthResult.Success(email, "uid-123"));

        var userRecord = new IdentityUserRecord(
            AggregateId: "staff-123",
            Username: "elena_r",
            Name: "Dr. Elena Rodriguez",
            Email: email,
            Phone: "+1555123456"
        );

        _userRepo.GetByNormalizedEmailAsync(email.ToLowerInvariant(), Arg.Any<CancellationToken>())
            .Returns(userRecord);

        _jwtGen.GenerateToken(userRecord, false)
            .Returns(("mock.jwt.token", 3600));

        var request = new LoginRequest
        {
            Email = email,
            Password = password,
            RememberMe = false
        };

        var (success, data, failureReason, errorDetail) = await sut.LoginAsync(request, "127.0.0.1");

        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Token.Should().Be("mock.jwt.token");
        data.User.Username.Should().Be("elena_r");
        data.User.Email.Should().Be(email);

        await _userRepo.Received(1).RecordLoginAsync("staff-123", Arg.Any<DateTimeOffset>(), "127.0.0.1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenIdPFails_ReturnsFailureWithoutQueryingDb()
    {
        var sut = CreateSut();
        var email = "test@rest.com";

        _externalIdp.AuthenticateAsync(email, "WrongPassword", Arg.Any<CancellationToken>())
            .Returns(ExternalAuthResult.Failed(AuthFailureReason.InvalidCredentials, "Invalid credentials."));

        var request = new LoginRequest
        {
            Email = email,
            Password = "WrongPassword"
        };

        var (success, data, failureReason, errorDetail) = await sut.LoginAsync(request, "127.0.0.1");

        success.Should().BeFalse();
        failureReason.Should().Be(AuthFailureReason.InvalidCredentials);
        data.Should().BeNull();

        await _userRepo.DidNotReceive().GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _jwtGen.DidNotReceive().GenerateToken(Arg.Any<IdentityUserRecord>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task LoginAsync_WhenIdPSucceedsButUserNotInDb_ReturnsCleanError()
    {
        var sut = CreateSut();
        var email = "stranger@external.com";

        _externalIdp.AuthenticateAsync(email, "ValidPassword123!", Arg.Any<CancellationToken>())
            .Returns(ExternalAuthResult.Success(email, "uid-999"));

        _userRepo.GetByNormalizedEmailAsync(email.ToLowerInvariant(), Arg.Any<CancellationToken>())
            .Returns((IdentityUserRecord?)null);

        var request = new LoginRequest
        {
            Email = email,
            Password = "ValidPassword123!"
        };

        var (success, data, failureReason, errorDetail) = await sut.LoginAsync(request, "127.0.0.1");

        success.Should().BeFalse();
        failureReason.Should().Be(AuthFailureReason.InvalidCredentials);
        errorDetail.Should().Contain("not registered");
        _jwtGen.DidNotReceive().GenerateToken(Arg.Any<IdentityUserRecord>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task ForgotPasswordAsync_AlwaysReturnsGenericMessage()
    {
        var sut = CreateSut();
        var request = new ForgotPasswordRequest { Email = "test@rest.com" };

        var response = await sut.ForgotPasswordAsync(request);

        response.Should().NotBeNull();
        response.Message.Should().Contain("password reset link has been dispatched");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenCodeValid_UpdatesAuditTimestamp()
    {
        var sut = CreateSut();
        _externalIdp.ConfirmPasswordResetAsync("valid-code", "NewPassword123!", Arg.Any<CancellationToken>())
            .Returns(PasswordResetResult.Success("test@rest.com"));

        var user = new IdentityUserRecord("staff-1", "test", "Test User", "test@rest.com", "123");
        _userRepo.GetByNormalizedEmailAsync("test@rest.com", Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ResetPasswordRequest
        {
            ResetCode = "valid-code",
            NewPassword = "NewPassword123!"
        };

        var (success, data, failureReason, errorDetail) = await sut.ResetPasswordAsync(request);

        success.Should().BeTrue();
        await _userRepo.Received(1).UpdatePasswordResetTimestampAsync("staff-1", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenIdPSucceeds_UpdatesAuditTimestamp()
    {
        var sut = CreateSut();
        _externalIdp.ChangePasswordAsync("test@rest.com", "Current123!", "New123456!", Arg.Any<CancellationToken>())
            .Returns(PasswordChangeResult.Success());

        var user = new IdentityUserRecord("staff-1", "test", "Test User", "test@rest.com", "123");
        _userRepo.GetByNormalizedEmailAsync("test@rest.com", Arg.Any<CancellationToken>())
            .Returns(user);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = "Current123!",
            NewPassword = "New123456!"
        };

        var (success, data, failureReason, errorDetail) = await sut.ChangePasswordAsync("test@rest.com", request);

        success.Should().BeTrue();
        await _userRepo.Received(1).UpdatePasswordResetTimestampAsync("staff-1", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
