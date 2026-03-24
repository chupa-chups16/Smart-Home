using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartHome.Api.Models;

namespace SmartHome.Api.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");

        var keyValue = jwtSettings["Key"];
        if (string.IsNullOrWhiteSpace(keyValue))
            throw new InvalidOperationException("Missing Jwt:Key configuration.");

        var issuer = jwtSettings["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("Missing Jwt:Issuer configuration.");

        var audience = jwtSettings["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Missing Jwt:Audience configuration.");

        if (!double.TryParse(jwtSettings["ExpireMinutes"], out var expireMinutes) || expireMinutes <= 0)
            expireMinutes = 60;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
