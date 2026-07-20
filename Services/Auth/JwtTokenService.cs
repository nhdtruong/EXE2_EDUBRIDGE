using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduBridge.Models;
using Microsoft.IdentityModel.Tokens;

namespace EduBridge.Services.Auth;

public interface IJwtTokenService
{
    ApiAccessToken CreateAccessToken(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ApiAccessToken CreateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("JWT key 'Jwt:Key' is not configured.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(
            _configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role.RoleCode)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));

        return new ApiAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt);
    }
}

public sealed record ApiAccessToken(string Value, DateTime ExpiresAtUtc);
