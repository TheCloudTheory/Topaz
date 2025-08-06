using System.Text.Json.Serialization;
using Azure.Core;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.KeyVault.Models;

internal sealed class KeyVaultResource
    : ArmResource<KeyVaultResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public KeyVaultResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public KeyVaultResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroup,
        string name,
        AzureLocation location,
        KeyVaultResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.KeyVault/vaults/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.KeyVault/vaults";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override KeyVaultResourceProperties Properties { get; init; }
}