using IdentityService.IntegrationTests.Clients;

namespace IdentityService.IntegrationTests.Fixtures;

public sealed class RunningApiFixture : IDisposable
{
    public HttpClient HttpClient { get; private set; }
    public IAuthApiClient AuthClient { get; private set; }
    public string TestEmail { get; }
    public string TestPassword { get; }
    public bool IsServiceAvailable { get; }

    public RunningApiFixture()
    {
        LoadEnvFile();

        TestEmail = Environment.GetEnvironmentVariable("Test_Email")
            ?? Environment.GetEnvironmentVariable("ITest_Email")
            ?? string.Empty;

        TestPassword = Environment.GetEnvironmentVariable("Test_Password")
            ?? Environment.GetEnvironmentVariable("ITest_Password")
            ?? string.Empty;

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
        IsServiceAvailable = CheckServiceAvailability(HttpClient);
    }

    private static void LoadEnvFile()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var dockerEnv = Path.Combine(currentDir.FullName, "docker", ".env");
            if (File.Exists(dockerEnv))
            {
                ApplyEnvFile(dockerEnv);
                return;
            }

            var rootEnv = Path.Combine(currentDir.FullName, ".env");
            if (File.Exists(rootEnv))
            {
                ApplyEnvFile(rootEnv);
                return;
            }

            currentDir = currentDir.Parent;
        }
    }

    private static void ApplyEnvFile(string filePath)
    {
        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static bool CheckServiceAvailability(HttpClient client)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var response = client.GetAsync("health", cts.Token).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        HttpClient.Dispose();
    }
}
