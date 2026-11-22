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

    [Fact(Skip = "Requires live running Auth service on port 8081 (Docker or local)")]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var request = new LoginRequest
        {
            Email = "test@rest.com",
            Password = "Password123!"
        };

        var response = await _fixture.AuthClient.LoginAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(Skip = "Requires live running Auth service on port 8081 (Docker or local)")]
    public async Task ForgotPassword_ReturnsOk()
    {
        var request = new ForgotPasswordRequest
        {
            Email = "test@rest.com"
        };

        var response = await _fixture.AuthClient.ForgotPasswordAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
