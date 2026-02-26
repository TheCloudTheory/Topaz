using Azure.Core;

namespace Topaz.Identity;

public sealed class AzureLocalCredential(string objectId, bool isForGraph = false) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var tokenString = JwtHelper.GenerateJwt(objectId, isForGraph);
        return new AccessToken(tokenString, DateTimeOffset.MaxValue);
    }
    
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var tokenString = JwtHelper.GenerateJwt(objectId, isForGraph);
        return ValueTask.FromResult(new AccessToken(tokenString, DateTimeOffset.MaxValue));
    }
}