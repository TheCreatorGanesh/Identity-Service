using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IdentityService.Core.Interfaces;
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
        services.AddScoped<IIdentityUserRepository, PostgresIdentityUserRepository>();

        return services;
    }
}
