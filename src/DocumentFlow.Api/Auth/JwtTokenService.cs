using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocumentFlow.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DocumentFlow.Api.Auth;

public sealed record JwtSettings(string Issuer, string Audience, string SigningKey, int LifetimeMinutes)
{
    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Auth:Jwt:Issuer"];
        var audience = configuration["Auth:Jwt:Audience"];
        var key = configuration["Auth:Jwt:SigningKey"];
        if (!int.TryParse(configuration["Auth:Jwt:LifetimeMinutes"], out var lifetime) || lifetime < 1 || lifetime > 1440 ||
            string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            key is null || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("JWT issuer, audience, signing key (at least 32 UTF-8 bytes), and lifetime (1–1440 minutes) must be configured.");
        return new JwtSettings(issuer, audience, key, lifetime);
    }
}

public sealed class JwtTokenService(JwtSettings settings)
{
    public string Create(ApplicationUser user, string role) => CreateWithExpiry(user, role).AccessToken;

    public IssuedJwt CreateWithExpiry(ApplicationUser user, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new Claim("name", user.UserName ?? string.Empty),
            new Claim("display_name", user.DisplayName),
            new Claim("role", role),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var signing = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(settings.LifetimeMinutes), signingCredentials: signing);
        return new IssuedJwt(new JwtSecurityTokenHandler().WriteToken(jwt), jwt.ValidTo);
    }
}

public sealed record IssuedJwt(string AccessToken, DateTime ExpiresAtUtc);
