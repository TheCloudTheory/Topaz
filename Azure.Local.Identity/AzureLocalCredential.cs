using Azure.Core;

namespace Azure.Local.Identity;

public sealed class AzureLocalCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken("azurelocal", DateTimeOffset.MaxValue);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new AccessToken("azurelocal", DateTimeOffset.MaxValue));
    }
}
