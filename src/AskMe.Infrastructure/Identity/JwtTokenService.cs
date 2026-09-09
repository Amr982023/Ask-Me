using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AskMe.Application.Common.Interfaces;
using AskMe.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AskMe.Infrastructure.Identity;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured. Set it via environment variable/secret.");

        var expires = DateTime.UtcNow.AddHours(double.Parse(jwtSection["ExpiryHours"] ?? "24"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
