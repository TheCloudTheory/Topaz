using System.Text.Json.Serialization;
using Azure.ResourceManager.Storage.Models;
using Topaz.ResourceManager;

namespace Topaz.Service.Storage.Models;

internal sealed class StorageAccountResource
    : ArmResource<StorageAccountProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public StorageAccountResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public StorageAccountResource(string subscriptionId,
        string resourceGroupName,
        string name,
        string location,
        ResourceSku sku,
        string kind,
        StorageAccountProperties properties)
    {
        Id = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{name}";
        Name = name;
        Location = location;
        Sku = sku;
        Kind = kind;
        Properties = properties;
        Keys =
        [
            new TopazStorageAccountKey("key1", Guid.NewGuid().ToString(), nameof(StorageAccountKeyPermission.Full), DateTimeOffset.Now),
            new TopazStorageAccountKey("key2", Guid.NewGuid().ToString(), nameof(StorageAccountKeyPermission.Full), DateTimeOffset.Now)
        ];
    }
    
    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type => "Microsoft.Storage/storageAccounts";
    public override string Location { get; init; }
    public override IDictionary<string, string> Tags { get; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override StorageAccountProperties Properties { get; init; }
    public TopazStorageAccountKey[] Keys { get; init; }
}