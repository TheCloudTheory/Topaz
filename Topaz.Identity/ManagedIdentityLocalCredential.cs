using Azure.Core;

namespace Topaz.Identity;

/// <summary>
/// Emulated credential used to represent "calling as managed identity" in tests.
/// Topaz's auth layer must map this token to a principal; the token value is intentionally opaque.
/// </summary>
public sealed class ManagedIdentityLocalCredential(Guid principalId) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => new(JwtHelper.GenerateJwt(principalId.ToString()), DateTimeOffset.MaxValue);

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => ValueTask.FromResult(new AccessToken(JwtHelper.GenerateJwt(principalId.ToString()), DateTimeOffset.MaxValue));
}