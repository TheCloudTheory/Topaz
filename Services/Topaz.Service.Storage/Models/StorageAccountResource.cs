using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.ResourceManager.Storage.Models;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

namespace Topaz.Service.Storage.Models;

internal sealed class StorageAccountResource
    : ArmResource<StorageAccountResourceProperties>
{
    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public StorageAccountResource()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public StorageAccountResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroupName,
        string name,
        AzureLocation location,
        ResourceSku sku,
        string kind,
        StorageAccountResourceProperties resourceProperties)
    {
        Id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{name}";
        Name = name;
        Location = location;
        Sku = sku;
        Kind = kind;
        Properties = resourceProperties;
        Keys =
        [
            new TopazStorageAccountKey("key1", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                nameof(StorageAccountKeyPermission.Full), DateTimeOffset.Now),
            new TopazStorageAccountKey("key2", Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                nameof(StorageAccountKeyPermission.Full), DateTimeOffset.Now)
        ];
    }

    public StorageAccountResource(SubscriptionIdentifier subscriptionId,
        ResourceGroupIdentifier resourceGroupName,
        string name,
        AzureLocation location,
        ResourceSku sku,
        string kind,
        StorageAccountResourceProperties resourceProperties,
        TopazStorageAccountKey[] existingKeys)
    {
        Id =
            $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Storage/storageAccounts/{name}";
        Name = name;
        Location = location;
        Sku = sku;
        Kind = kind;
        Properties = resourceProperties;
        Keys = existingKeys;
    }

    public override string Id { get; init; }
    public override string Name { get; init; }
    public override string Type { get; init; } = "Microsoft.Storage/storageAccounts";
    public override string? Location { get; set; }
    public override IDictionary<string, string>? Tags { get; set; } = new Dictionary<string, string>();
    public override ResourceSku? Sku { get; init; }
    public override string? Kind { get; init; }
    public override StorageAccountResourceProperties Properties { get; init; }
    public TopazStorageAccountKey[] Keys { get; init; }

    /// <summary>
    /// Serializes the storage account for HTTP responses. Keys are intentionally excluded
    /// because the real Azure ARM API never returns keys in account GET/PUT/PATCH responses —
    /// callers must use the listKeys action to obtain them.
    /// </summary>
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            id = Id,
            name = Name,
            type = Type,
            location = Location,
            tags = Tags,
            sku = Sku,
            kind = Kind,
            properties = Properties
        }, Topaz.Shared.GlobalSettings.JsonOptions);
    }
}