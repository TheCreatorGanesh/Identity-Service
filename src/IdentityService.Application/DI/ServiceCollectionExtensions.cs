using Microsoft.Extensions.DependencyInjection;
using IdentityService.Application.Interfaces;
using IdentityService.Application.Services;

namespace IdentityService.Application.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
