using System.IdentityModel.Tokens.Jwt;
using Azure.Core;
using NUnit.Framework;
using Topaz.Identity;

namespace Topaz.Tests.Identity;

public class AzureLocalCredentialTests
{
    [Test]
    public void GetToken_ExpiresOnMatchesJwtExpiration()
    {
        var credential = new AzureLocalCredential("principal-id");

        var accessToken = credential.GetToken(new TokenRequestContext([]), CancellationToken.None);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);

        Assert.That(accessToken.ExpiresOn.UtcDateTime, Is.EqualTo(jwt.ValidTo));
        Assert.That(accessToken.ExpiresOn, Is.LessThan(DateTimeOffset.UtcNow.AddHours(1)));
    }
}
