namespace Topaz.Service.ManagedIdentity.Models;

public sealed class FederatedIdentityCredentialResourceProperties
{
    public required string Issuer { get; set; }
    public required string Subject { get; set; }
    public required IList<string> Audiences { get; set; }
}
