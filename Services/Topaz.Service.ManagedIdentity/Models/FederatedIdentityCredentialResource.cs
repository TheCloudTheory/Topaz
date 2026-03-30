using System.Text.Json.Serialization;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.ManagedIdentity.Models;

public sealed class FederatedIdentityCredentialResource
    : ArmSubresource<FederatedIdentityCredentialResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618
    public FederatedIdentityCredentialResource()
#pragma warning restore CS8618
    {
    }

    public FederatedIdentityCredentialResource(
        SubscriptionIdentifier subscription,
        ResourceGroupIdentifier resourceGroup,
        string managedIdentityName,
        string name,
        FederatedIdentityCredentialResourceProperties properties)
    {
        Id = $"/subscriptions/{subscription}/resourceGroups/{resourceGroup}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/{managedIdentityName}/federatedIdentityCredentials/{name}";
        Name = name;
        Properties = properties;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials";
    public override FederatedIdentityCredentialResourceProperties Properties { get; init; }
}
