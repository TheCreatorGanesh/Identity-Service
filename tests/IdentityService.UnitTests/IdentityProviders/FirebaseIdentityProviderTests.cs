using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IdentityService.Core.Configuration;
using IdentityService.Core.Enums;
using IdentityService.Core.IdentityProviders.Firebase;
using Xunit;

namespace IdentityService.UnitTests.IdentityProviders;

public sealed class FirebaseIdentityProviderTests
{
    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handlerFunc(request));
        }
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSuccessful_ReturnsSuccessResult()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            req.RequestUri!.ToString().Should().Contain("accounts:signInWithPassword");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    idToken = "firebase-test-id-token",
                    email = "test@rest.com",
                    localId = "uid-12345"
                }))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/") };
        var options = Options.Create(new FirebaseOptions
        {
            ApiKey = "test-key",
            BypassSignatureValidation = false
        });

        var idp = new FirebaseIdentityProvider(client, options, NullLogger<FirebaseIdentityProvider>.Instance);

        var result = await idp.AuthenticateAsync("test@rest.com", "Password123!");

        result.IsSuccess.Should().BeTrue();
        result.Email.Should().Be("test@rest.com");
        result.ProviderUserId.Should().Be("uid-12345");
        result.FailureReason.Should().Be(AuthFailureReason.None);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenInvalidCredentials_ReturnsFailedResult()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        code = 400,
                        message = "INVALID_LOGIN_CREDENTIALS"
                    }
                }))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/") };
        var options = Options.Create(new FirebaseOptions
        {
            ApiKey = "test-key",
            BypassSignatureValidation = false
        });

        var idp = new FirebaseIdentityProvider(client, options, NullLogger<FirebaseIdentityProvider>.Instance);

        var result = await idp.AuthenticateAsync("wrong@rest.com", "BadPassword");

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(AuthFailureReason.InvalidCredentials);
    }

    [Fact]
    public async Task SendPasswordResetAsync_WhenEmailNotFound_TreatsNeutrallyAndReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    error = new
                    {
                        code = 400,
                        message = "EMAIL_NOT_FOUND"
                    }
                }))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/") };
        var options = Options.Create(new FirebaseOptions
        {
            ApiKey = "test-key",
            BypassSignatureValidation = false
        });

        var idp = new FirebaseIdentityProvider(client, options, NullLogger<FirebaseIdentityProvider>.Instance);

        var result = await idp.SendPasswordResetAsync("nonexistent@rest.com");

        result.IsSuccess.Should().BeTrue();
        result.FailureReason.Should().Be(AuthFailureReason.None);
    }

    [Fact]
    public async Task ConfirmPasswordResetAsync_WhenValidCode_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    email = "test@rest.com",
                    requestType = "PASSWORD_RESET"
                }))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/") };
        var options = Options.Create(new FirebaseOptions
        {
            ApiKey = "test-key",
            BypassSignatureValidation = false
        });

        var idp = new FirebaseIdentityProvider(client, options, NullLogger<FirebaseIdentityProvider>.Instance);

        var result = await idp.ConfirmPasswordResetAsync("valid-oob-code", "NewPassword123!");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_ExecutesSignInThenUpdateSuccessfully()
    {
        var callCount = 0;
        var handler = new MockHttpMessageHandler(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                req.RequestUri!.ToString().Should().Contain("accounts:signInWithPassword");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        idToken = "fresh-id-token",
                        email = "test@rest.com"
                    }))
                };
            }

            req.RequestUri!.ToString().Should().Contain("accounts:update");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    localId = "uid-12345",
                    email = "test@rest.com"
                }))
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://identitytoolkit.googleapis.com/v1/") };
        var options = Options.Create(new FirebaseOptions
        {
            ApiKey = "test-key",
            BypassSignatureValidation = false
        });

        var idp = new FirebaseIdentityProvider(client, options, NullLogger<FirebaseIdentityProvider>.Instance);

        var result = await idp.ChangePasswordAsync("test@rest.com", "CurrentPassword123!", "NewPassword123!");

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(2);
    }
}
