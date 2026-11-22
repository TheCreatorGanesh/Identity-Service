using System.Net;
using FluentAssertions;
using IdentityService.Core.DTOs;
using IdentityService.IntegrationTests.Fixtures;
using Xunit;

namespace IdentityService.IntegrationTests.Tests;

public sealed class AuthControllerTests : IClassFixture<RunningApiFixture>
{
    private readonly RunningApiFixture _fixture;

    public AuthControllerTests(RunningApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        if (!_fixture.IsServiceAvailable)
        {
            // Requires live running Auth service on port 8081 (Docker or local)
            return;
        }

        if (string.IsNullOrWhiteSpace(_fixture.TestEmail) || string.IsNullOrWhiteSpace(_fixture.TestPassword))
        {
            throw new InvalidOperationException("Test_Email and Test_Password must be configured in the .env file.");
        }

        var request = new LoginRequest
        {
            Email = _fixture.TestEmail,
            Password = _fixture.TestPassword
        };

        var response = await _fixture.AuthClient.LoginAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk()
    {
        if (!_fixture.IsServiceAvailable)
        {
            // Requires live running Auth service on port 8081 (Docker or local)
            return;
        }

        if (string.IsNullOrWhiteSpace(_fixture.TestEmail))
        {
            throw new InvalidOperationException("Test_Email must be configured in the .env file.");
        }

        var request = new ForgotPasswordRequest
        {
            Email = _fixture.TestEmail
        };

        var response = await _fixture.AuthClient.ForgotPasswordAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
