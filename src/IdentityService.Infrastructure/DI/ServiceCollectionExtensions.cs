using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityService.Core.Interfaces;
using IdentityService.Infrastructure.Migrations;
using IdentityService.Infrastructure.Repositories;

namespace IdentityService.Infrastructure.DI;

/// <summary>
/// Registers infrastructure database and data access dependencies into the ASP.NET Core DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Infrastructure is focused exclusively on DB access and persistence
        services.AddSingleton<PostgresMigrationRunner>();
        services.AddScoped<IIdentityUserRepository, PostgresIdentityUserRepository>();

        return services;
    }

    /// <summary>
    /// Applies pending database migrations at application startup.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var runner = services.GetRequiredService<PostgresMigrationRunner>();
        await runner.ApplyMigrationsAsync(cancellationToken);
    }
}
