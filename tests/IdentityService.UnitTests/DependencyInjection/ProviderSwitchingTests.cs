using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IdentityService.Core.Configuration;
using IdentityService.Core.Interfaces;
using IdentityService.Core.DI;
using IdentityService.Core.IdentityProviders.Firebase;
using IdentityService.Core.IdentityProviders.LocalStub;
using Xunit;

namespace IdentityService.UnitTests.DependencyInjection;

public sealed class ProviderSwitchingTests
{
    [Fact]
    public void AddExternalIdentityProvider_WhenConfiguredForFirebase_ResolvesFirebaseIdentityProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentityProvider:ActiveProvider"] = "Firebase",
                ["Firebase:ProjectId"] = "test-project",
                ["Firebase:ApiKey"] = "test-key",
                ["Firebase:BaseUrl"] = "https://identitytoolkit.googleapis.com/v1"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<FirebaseOptions>(config.GetSection("Firebase"));
        services.AddExternalIdentityProvider(config);

        var provider = services.BuildServiceProvider();
        var idp = provider.GetRequiredService<IExternalIdentityProvider>();

        idp.Should().BeOfType<FirebaseIdentityProvider>();
    }

    [Fact]
    public void AddExternalIdentityProvider_WhenConfiguredForLocalStub_ResolvesLocalStubIdentityProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentityProvider:ActiveProvider"] = "LocalStub"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternalIdentityProvider(config);

        var provider = services.BuildServiceProvider();
        var idp = provider.GetRequiredService<IExternalIdentityProvider>();

        idp.Should().BeOfType<LocalStubIdentityProvider>();
    }

    [Fact]
    public void AddExternalIdentityProvider_WhenConfiguredWithUnsupportedProvider_ThrowsInvalidOperationException()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IdentityProvider:ActiveProvider"] = "UnknownIdP"
            })
            .Build();

        var services = new ServiceCollection();

        var act = () => services.AddExternalIdentityProvider(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsupported IdentityProvider:ActiveProvider value 'UnknownIdP'*");
    }
}
