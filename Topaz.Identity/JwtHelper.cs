using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Topaz.Identity;

public static class JwtHelper
{
    private static readonly byte[] SecretKey =
        "yD1sMV1WcwVjSfNUxxLNfVHn5sbqD056LwOnkXCkIDnWkXcrg95plLQ3T1tvinLAnuNNiRRZrKyUvs6YzZnJ/A=="u8.ToArray();

    internal static string GenerateJwt(string objectId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
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
    
    public static JwtSecurityToken? ValidateJwt(string jwt)
    {
        jwt = NormalizeBearerToken(jwt);
        
        var tokenHandler = new JwtSecurityTokenHandler();

        tokenHandler.ValidateToken(jwt, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(SecretKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        }, out var validatedToken);

        return (JwtSecurityToken)validatedToken;
    }
    
    private static string NormalizeBearerToken(string? token)
    {
        token = token?.Trim();

        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        const string bearerPrefix = "Bearer ";
        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            token = token[bearerPrefix.Length..].Trim();

        // Common copy/paste / serialization artifacts
        token = token.Trim().Trim('"');

        return token;
    }
}