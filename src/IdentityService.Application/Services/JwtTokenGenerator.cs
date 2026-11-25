using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Application.Interfaces;
using IdentityService.Core.Configuration;
using IdentityService.Core.Models;

namespace IdentityService.Application.Services;

/// <summary>
/// ASP.NET Core JWT token generator minting sub and email claims.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public (string TokenString, int ExpiresInSeconds) GenerateToken(IdentityUserRecord user, bool rememberMe)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Key);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.AggregateId),
            new(ClaimTypes.NameIdentifier, user.AggregateId),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("email", user.Email),
            new("name", user.Name)
        };

        var expires = DateTime.UtcNow.AddHours(rememberMe ? 168 : 1);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        var expiresInSeconds = (int)(expires - DateTime.UtcNow).TotalSeconds;

        return (tokenString, expiresInSeconds);
    }
}
