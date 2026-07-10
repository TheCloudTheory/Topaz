using System.Security.Cryptography;

namespace Topaz.Service.AppService.Models;

internal sealed class PublishingCredentialsResourceProperties
{
    public string? PublishingPassword { get; set; }
    public string? PublishingPasswordHash { get; set; }
    public string? PublishingPasswordHashSalt { get; set; }
    public string? ScmUri { get; set; } = string.Empty;
    public string? PublishingUserName { get; set; } = string.Empty;

    public static PublishingCredentialsResourceProperties Create(string userName, string scmUri)
    {
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password + salt)));

        return new PublishingCredentialsResourceProperties()
        {
            PublishingUserName = userName,
            PublishingPassword = password,
            PublishingPasswordHash = hash,
            PublishingPasswordHashSalt = salt,
            ScmUri = scmUri,
        };
    }
}