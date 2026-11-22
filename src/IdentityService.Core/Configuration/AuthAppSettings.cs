namespace IdentityService.Core.Configuration;

/// <summary>
/// Root application settings matching existing SystemAdmissions API structure and validation style.
/// </summary>
public sealed class AuthAppSettings
{
    public const string SectionName = "AppSettings";

    private ConnectionStringsOptions _connectionStrings = new();
    private CorsOptions _cors = new();
    private JwtOptions _jwt = new();
    private LoggingOptions _logging = new();
    private IdentityProviderOptions _identityProvider = new();
    private FirebaseOptions _firebase = new();
    private string _allowedHosts = "*";

    public ConnectionStringsOptions ConnectionStrings
    {
        get => _connectionStrings ?? throw new InvalidOperationException("Configuration section 'ConnectionStrings' is missing or null.");
        set => _connectionStrings = value ?? new();
    }

    public CorsOptions Cors
    {
        get => _cors ?? throw new InvalidOperationException("Configuration section 'Cors' is missing or null.");
        set => _cors = value ?? new();
    }

    public JwtOptions Jwt
    {
        get => _jwt ?? throw new InvalidOperationException("Configuration section 'Jwt' is missing or null.");
        set => _jwt = value ?? new();
    }

    public LoggingOptions Logging
    {
        get => _logging ?? throw new InvalidOperationException("Configuration section 'Logging' is missing or null.");
        set => _logging = value ?? new();
    }

    public IdentityProviderOptions IdentityProvider
    {
        get => _identityProvider ?? throw new InvalidOperationException("Configuration section 'IdentityProvider' is missing or null.");
        set => _identityProvider = value ?? new();
    }

    public FirebaseOptions Firebase
    {
        get => _firebase ?? throw new InvalidOperationException("Configuration section 'Firebase' is missing or null.");
        set => _firebase = value ?? new();
    }

    public string AllowedHosts
    {
        get => string.IsNullOrWhiteSpace(_allowedHosts)
            ? throw new InvalidOperationException("Configuration value 'AllowedHosts' is missing or empty.")
            : _allowedHosts;
        set => _allowedHosts = value;
    }

    public void Validate()
    {
        _ = ConnectionStrings.PostgreSQL;
        _ = Cors.AllowedOrigins;
        _ = Jwt.Key;
        _ = Jwt.Issuer;
        _ = Jwt.Audience;
        _ = AllowedHosts;
        _ = IdentityProvider.ActiveProvider;

        if (string.Equals(IdentityProvider.ActiveProvider, "Firebase", StringComparison.OrdinalIgnoreCase))
        {
            if (!Firebase.BypassSignatureValidation)
            {
                _ = Firebase.ApiKey;
            }
            _ = Firebase.ProjectId;
            _ = Firebase.BaseUrl;
            _ = Firebase.SignInEndpoint;
            _ = Firebase.UpdateEndpoint;
            _ = Firebase.SendOobCodeEndpoint;
            _ = Firebase.ResetPasswordEndpoint;
        }
    }
}

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    private string _postgreSQL = string.Empty;

    public string PostgreSQL
    {
        get => string.IsNullOrWhiteSpace(_postgreSQL)
            ? throw new InvalidOperationException("Missing required configuration: 'ConnectionStrings:PostgreSQL' is null or empty.")
            : _postgreSQL;
        set => _postgreSQL = value;
    }
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    private string[] _allowedOrigins = ["http://localhost:4200"];

    public string[] AllowedOrigins
    {
        get => _allowedOrigins == null || _allowedOrigins.Length == 0
            ? throw new InvalidOperationException("Missing required configuration: 'Cors:AllowedOrigins' is null or empty.")
            : _allowedOrigins;
        set => _allowedOrigins = value ?? [];
    }
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private string _key = string.Empty;
    private string _issuer = "http://localhost:8080";
    private string _audience = "http://localhost:4200";

    public string Key
    {
        get => string.IsNullOrWhiteSpace(_key)
            ? throw new InvalidOperationException("Missing required configuration: 'Jwt:Key' is null or empty.")
            : _key;
        set => _key = value;
    }

    public string Issuer
    {
        get => string.IsNullOrWhiteSpace(_issuer)
            ? throw new InvalidOperationException("Missing required configuration: 'Jwt:Issuer' is null or empty.")
            : _issuer;
        set => _issuer = value;
    }

    public string Audience
    {
        get => string.IsNullOrWhiteSpace(_audience)
            ? throw new InvalidOperationException("Missing required configuration: 'Jwt:Audience' is null or empty.")
            : _audience;
        set => _audience = value;
    }
}

public sealed class LoggingOptions
{
    public const string SectionName = "Logging";

    public Dictionary<string, string> LogLevel { get; set; } = new();
}

public sealed class IdentityProviderOptions
{
    public const string SectionName = "IdentityProvider";

    private string _activeProvider = "Firebase";

    /// <summary>
    /// The active identity provider to use: "Firebase" or "LocalStub".
    /// </summary>
    public string ActiveProvider
    {
        get => string.IsNullOrWhiteSpace(_activeProvider)
            ? throw new InvalidOperationException("Missing required configuration: 'IdentityProvider:ActiveProvider' is null or empty.")
            : _activeProvider;
        set => _activeProvider = value;
    }
}

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    private string _projectId = string.Empty;
    private string _apiKey = string.Empty;
    private string _baseUrl = "https://identitytoolkit.googleapis.com/v1";
    private string _signInEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
    private string _updateEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:update";
    private string _sendOobCodeEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode";
    private string _resetPasswordEndpoint = "https://identitytoolkit.googleapis.com/v1/accounts:resetPassword";

    public string ProjectId
    {
        get => string.IsNullOrWhiteSpace(_projectId)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:ProjectId' is null or empty.")
            : _projectId;
        set => _projectId = value;
    }

    public bool BypassSignatureValidation { get; set; }

    public string ApiKey
    {
        get
        {
            if (!BypassSignatureValidation && string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException("Missing required configuration: 'Firebase:ApiKey' is null or empty when signature validation is enabled.");
            }
            return _apiKey;
        }
        set => _apiKey = value;
    }

    public string BaseUrl
    {
        get => string.IsNullOrWhiteSpace(_baseUrl)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:BaseUrl' is null or empty.")
            : _baseUrl;
        set => _baseUrl = value;
    }

    public string SignInEndpoint
    {
        get => string.IsNullOrWhiteSpace(_signInEndpoint)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:SignInEndpoint' is null or empty.")
            : _signInEndpoint;
        set => _signInEndpoint = value;
    }

    public string UpdateEndpoint
    {
        get => string.IsNullOrWhiteSpace(_updateEndpoint)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:UpdateEndpoint' is null or empty.")
            : _updateEndpoint;
        set => _updateEndpoint = value;
    }

    public string SendOobCodeEndpoint
    {
        get => string.IsNullOrWhiteSpace(_sendOobCodeEndpoint)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:SendOobCodeEndpoint' is null or empty.")
            : _sendOobCodeEndpoint;
        set => _sendOobCodeEndpoint = value;
    }

    public string ResetPasswordEndpoint
    {
        get => string.IsNullOrWhiteSpace(_resetPasswordEndpoint)
            ? throw new InvalidOperationException("Missing required configuration: 'Firebase:ResetPasswordEndpoint' is null or empty.")
            : _resetPasswordEndpoint;
        set => _resetPasswordEndpoint = value;
    }

    public string GetSignInUrl() => $"{SignInEndpoint}?key={ApiKey}";
    public string GetUpdateUrl() => $"{UpdateEndpoint}?key={ApiKey}";
    public string GetSendOobCodeUrl() => $"{SendOobCodeEndpoint}?key={ApiKey}";
    public string GetResetPasswordUrl() => $"{ResetPasswordEndpoint}?key={ApiKey}";
}
