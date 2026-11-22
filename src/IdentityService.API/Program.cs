using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Scalar.AspNetCore;
using IdentityService.Application.DI;
using IdentityService.Core.Configuration;
using IdentityService.Core.DI;
using IdentityService.Core.Interfaces;
using IdentityService.Infrastructure.DI;

// ── Load .env file if present (matching Admissions API pattern) ──────────────
LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// ── Configuration Binding & Options Pattern ──────────────────────────────────
builder.Services.Configure<AuthAppSettings>(builder.Configuration);
builder.Services.Configure<ConnectionStringsOptions>(builder.Configuration.GetSection(ConnectionStringsOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<IdentityProviderOptions>(builder.Configuration.GetSection(IdentityProviderOptions.SectionName));
builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));

var appSettings = builder.Configuration.Get<AuthAppSettings>() ?? new AuthAppSettings();
builder.Services.AddSingleton(appSettings);

// ── Logging ──────────────────────────────────────────────────────────────────
builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();

// ── MVC & Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// ── API Versioning ───────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Application & Infrastructure Services ────────────────────────────────────
builder.Services.AddExternalIdentityProvider(builder.Configuration);
builder.Services.AddIdentityServiceInfrastructure(builder.Configuration);
builder.Services.AddIdentityServiceApplication();

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(appSettings.Cors.AllowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ── Authentication (JWT Bearer) ──────────────────────────────────────────────
var keyBytes = Encoding.UTF8.GetBytes(appSettings.Jwt.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = appSettings.Jwt.Issuer,
        ValidAudience = appSettings.Jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

builder.Services.AddAuthorization();

// ── OpenAPI (built-in .NET 10 + Scalar UI) ───────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "Identity Service API",
            Version = "v1",
            Description = "Decoupled, configuration-driven authentication service for Identity Service."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes.TryAdd("BearerAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Provide JWT Access Token."
        });

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, ct) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var hasAuthorize = metadata.Any(m => m is AuthorizeAttribute);
        var hasAllowAnonymous = metadata.Any(m => m is AllowAnonymousAttribute);

        if (hasAuthorize && !hasAllowAnonymous)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("BearerAuth", context.Document),
                    new List<string>()
                }
            });
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddSwaggerGen();

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "🔐 Identity Service API";
        options.Theme = ScalarTheme.DeepSpace;
    });

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "🔐 Identity Service API";
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Health Check Endpoint ─────────────────────────────────────────────────────
app.MapGet("/health", async (
    AuthAppSettings settings,
    IExternalIdentityProvider idp,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var dependencies = new Dictionary<string, string>
    {
        { "database", "Unknown" },
        { "identityProvider", settings.IdentityProvider.ActiveProvider }
    };

    // Test database connection
    try
    {
        await using var connection = new NpgsqlConnection(settings.ConnectionStrings.PostgreSQL);
        await connection.OpenAsync(ct);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        _ = await command.ExecuteScalarAsync(ct);
        dependencies["database"] = "Connected";
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[HealthCheck] Database check failed");
        dependencies["database"] = $"Failed: {ex.Message}";

        return Results.Json(new
        {
            status = "Unhealthy",
            service = "IdentityService.API",
            version = "1.0.0",
            timestamp = DateTimeOffset.UtcNow,
            dependencies,
            reason = "Database connectivity check failed"
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new
    {
        status = "Healthy",
        service = "IdentityService.API",
        version = "1.0.0",
        timestamp = DateTimeOffset.UtcNow,
        dependencies
    }, statusCode: StatusCodes.Status200OK);
});

app.Run();

// ── Helpers ──────────────────────────────────────────────────────────────────
static void LoadDotEnv()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env")
    };

    foreach (var path in candidates)
    {
        if (File.Exists(path))
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex > 0)
                {
                    var key = trimmed[..separatorIndex].Trim();
                    var val = trimmed[(separatorIndex + 1)..].Trim();
                    if ((val.StartsWith('"') && val.EndsWith('"')) || (val.StartsWith('\'') && val.EndsWith('\'')))
                    {
                        val = val[1..^1];
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
            }
            break;
        }
    }
}

public partial class Program { }
