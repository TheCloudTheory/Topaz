using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Topaz.Identity;

public static class JwtHelper
{
    private static readonly byte[] SecretKey = "azurelocal"u8.ToArray();

    internal static string GenerateJwt(string objectId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = "your-32-byte-secret-key-here"u8.ToArray();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", objectId)]),
            Issuer = "https://topaz.local.dev:8899",
            Audience = "https://topaz.local.dev:8899",
            NotBefore = DateTime.UtcNow,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(SecretKey), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);
        return tokenString;
    }
}