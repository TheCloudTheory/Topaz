using System.Text.Json.Serialization;
using Topaz.ResourceManager;

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

    public KeyVaultResource(string subscriptionId,
        string resourceGroupName,
        string name,
        string location,
        KeyVaultResourceProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{name}";
        Name = name;
        Location = location;
        Properties = properties;
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.KeyVault/vaults";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    public override KeyVaultResourceProperties Properties { get; init; }
}