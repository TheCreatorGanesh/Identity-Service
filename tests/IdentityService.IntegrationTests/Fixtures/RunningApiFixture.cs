using IdentityService.IntegrationTests.Clients;

namespace IdentityService.IntegrationTests.Fixtures;

public sealed class RunningApiFixture : IDisposable
{
    public HttpClient HttpClient { get; private set; }
    public IAuthApiClient AuthClient { get; private set; }

    public RunningApiFixture()
    {
        var baseAddress = Environment.GetEnvironmentVariable("IDENTITY_SERVICE_API_URL") ?? "http://localhost:8081";

        HttpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        {
            BaseAddress = new Uri(baseAddress.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

        AuthClient = RefitClientFactory.Create<IAuthApiClient>(HttpClient);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
