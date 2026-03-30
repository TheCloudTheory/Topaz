namespace Topaz.Service.ManagedIdentity.Models.Requests;

public sealed class CreateOrUpdateFederatedIdentityCredentialRequest
{
    public FederatedIdentityCredentialProperties? Properties { get; init; }

    public sealed class FederatedIdentityCredentialProperties
    {
        public required string Issuer { get; init; }
        public required string Subject { get; init; }
        public required IList<string> Audiences { get; init; }
    }
}
