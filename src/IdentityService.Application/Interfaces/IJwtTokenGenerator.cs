using IdentityService.Core.Models;

namespace IdentityService.Application.Interfaces;

/// <summary>
/// Mints ASP.NET Core JWT tokens for authenticated users with standard and custom claims.
/// </summary>
public interface IJwtTokenGenerator
{
    (string TokenString, int ExpiresInSeconds) GenerateToken(IdentityUserRecord user, bool rememberMe);
}
