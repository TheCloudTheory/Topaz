using Azure.Core;

namespace Topaz.Identity;

public sealed class AzureLocalCredential(string objectId, bool isForGraph = false, string? preferredUsername = null)
    : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => JwtHelper.GenerateLocalAccessToken(objectId, isForGraph, preferredUsername);

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => ValueTask.FromResult(JwtHelper.GenerateLocalAccessToken(objectId, isForGraph, preferredUsername));
}

public sealed class AzureFixedTokenLocalCredential(string token) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken(token, DateTimeOffset.MaxValue);
    }
    
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new AccessToken(token, DateTimeOffset.MaxValue));
    }
}