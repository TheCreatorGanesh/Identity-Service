using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IdentityService.Core.Configuration;
using IdentityService.Core.IdentityProviders.Firebase;
using IdentityService.Core.IdentityProviders.LocalStub;
using IdentityService.Core.Interfaces;

namespace IdentityService.Core.DI;

/// <summary>
/// Handles configuration-driven registration and dynamic swapping of external identity providers.
/// </summary>
public static class IdentityProviderRegistrationExtensions
{
    public static IServiceCollection AddExternalIdentityProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var activeProvider = configuration.GetSection("IdentityProvider:ActiveProvider").Value
            ?? configuration.GetSection("AppSettings:IdentityProvider:ActiveProvider").Value;

        if (string.IsNullOrWhiteSpace(activeProvider))
        {
            throw new InvalidOperationException(
                "Missing required configuration: 'IdentityProvider:ActiveProvider' is null or empty. " +
                "Supported values are 'Firebase' or 'LocalStub'.");
        }

        switch (activeProvider.Trim().ToLowerInvariant())
        {
            case "firebase":
                services.AddHttpClient<IExternalIdentityProvider, FirebaseIdentityProvider>((sp, client) =>
                {
                    var fbOptions = sp.GetRequiredService<IOptions<FirebaseOptions>>().Value;
                    client.BaseAddress = new Uri(fbOptions.BaseUrl.TrimEnd('/') + "/");
                    client.Timeout = TimeSpan.FromSeconds(15);
                });
                break;

            case "localstub":
                services.AddSingleton<IExternalIdentityProvider, LocalStubIdentityProvider>();
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported IdentityProvider:ActiveProvider value '{activeProvider}'. " +
                    "Supported providers are 'Firebase' and 'LocalStub'.");
        }

        return services;
    }
}
