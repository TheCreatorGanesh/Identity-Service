using Refit;

namespace IdentityService.IntegrationTests.Clients;

internal static class RefitClientFactory
{
    private static readonly RefitSettings Settings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer()
    };

    public static T Create<T>(HttpClient httpClient) =>
        RestService.For<T>(httpClient, Settings);
}
