using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SpacePortalBackEnd.Security;

public interface IJwtTokenService
{
    (string token, DateTime expiresUtc) Create(string email, long userId, IEnumerable<string> roles);
}

public class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    public (string token, DateTime expiresUtc) Create(string email, long userId, IEnumerable<string> roles)
    {
        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;
        var keyBytes = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(int.TryParse(config["Jwt:AccessTokenMinutes"], out var m) ? m : 30);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email)
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var token = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow, expires, creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
