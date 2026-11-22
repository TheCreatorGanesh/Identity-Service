using FluentAssertions;
using IdentityService.Core.Configuration;
using Xunit;

namespace IdentityService.UnitTests.Configuration;

public sealed class AuthAppSettingsTests
{
    [Fact]
    public void AuthAppSettings_DefaultValues_AreValid()
    {
        var settings = new AuthAppSettings
        {
            ConnectionStrings = new ConnectionStringsOptions
            {
                PostgreSQL = "Host=localhost;Port=5432;Database=IdentityManagement;Username=postgres;Password=secret"
            },
            Jwt = new JwtOptions
            {
                Key = "SuperSecretKeyForAuthTesting1234567890!"
            },
            IdentityProvider = new IdentityProviderOptions
            {
                ActiveProvider = "Firebase"
            },
            Firebase = new FirebaseOptions
            {
                ProjectId = "identity-service-a4db0",
                ApiKey = "AIzaSyTestKey123",
                BypassSignatureValidation = false
            }
        };

        var act = () => settings.Validate();
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ConnectionStrings_EmptyPostgreSQL_ThrowsInvalidOperationException(string? invalidConn)
    {
        var options = new ConnectionStringsOptions();
        if (invalidConn != null)
        {
            options.PostgreSQL = invalidConn;
        }

        var act = () => _ = options.PostgreSQL;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'ConnectionStrings:PostgreSQL'*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void JwtOptions_EmptyKey_ThrowsInvalidOperationException(string invalidKey)
    {
        var options = new JwtOptions { Key = invalidKey };
        var act = () => _ = options.Key;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'Jwt:Key'*");
    }

    [Fact]
    public void FirebaseOptions_EmptyApiKey_WhenSignatureValidationEnabled_ThrowsInvalidOperationException()
    {
        var options = new FirebaseOptions
        {
            BypassSignatureValidation = false,
            ApiKey = ""
        };

        var act = () => _ = options.ApiKey;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'Firebase:ApiKey'*");
    }

    [Fact]
    public void FirebaseOptions_EmptyApiKey_WhenSignatureValidationBypassed_DoesNotThrow()
    {
        var options = new FirebaseOptions
        {
            BypassSignatureValidation = true,
            ApiKey = ""
        };

        var act = () => _ = options.ApiKey;
        act.Should().NotThrow();
    }
}
