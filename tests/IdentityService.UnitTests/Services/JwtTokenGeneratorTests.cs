using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using IdentityService.Application.Services;
using IdentityService.Core.Configuration;
using IdentityService.Core.Models;
using Xunit;

namespace IdentityService.UnitTests.Services;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_MintsRequiredClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "SuperSecretKeyForAuthTestingJwtMinting123!",
            Issuer = "http://localhost:8080",
            Audience = "http://localhost:4200"
        });

        var sut = new JwtTokenGenerator(options);

        var user = new IdentityUserRecord(
            AggregateId: "staff-aggregate-99",
            Username: "erodriguez_staff",
            Name: "Dr. Elena Rodriguez",
            Email: "e.rodriguez@academic.edu",
            Phone: "+1 (555) 019-2834"
        );

        var (tokenString, expiresInSeconds) = sut.GenerateToken(user, rememberMe: false);

        tokenString.Should().NotBeNullOrWhiteSpace();
        expiresInSeconds.Should().BeInRange(3500, 3600);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        jwt.Issuer.Should().Be("http://localhost:8080");
        jwt.Audiences.Should().Contain("http://localhost:4200");

        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "staff-aggregate-99");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "e.rodriguez@academic.edu");
    }

    [Fact]
    public void GenerateToken_WithRememberMe_SetsExtendedExpiry()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "SuperSecretKeyForAuthTestingJwtMinting123!",
            Issuer = "http://localhost:8080",
            Audience = "http://localhost:4200"
        });

        var sut = new JwtTokenGenerator(options);

        var user = new IdentityUserRecord("staff-1", "user", "User", "user@example.com", "123");

        var (_, expiresInSeconds) = sut.GenerateToken(user, rememberMe: true);

        // 168 hours = 604800 seconds
        expiresInSeconds.Should().BeInRange(604000, 604800);
    }
}
