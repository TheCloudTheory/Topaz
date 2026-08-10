using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Service.Storage.Models.Requests;

internal record CreateOrUpdateStorageAccountRequest
{
    public ResourceSku? Sku { get; init; }
    public string? Kind { get; init; }
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; init; }
    public ManagedServiceIdentity? Identity { get; init; }
    public StorageAccountResourceProperties? Properties { get; init; }

    public static CreateOrUpdateStorageAccountRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateStorageAccountRequest
        {
            Identity = data.Identity,
            Location = data.Location,
            Tags = data.Tags,
            Sku = new ResourceSku
            {
                Capacity = data.Sku?.Capacity,
                Name = data.Sku?.Name,
                Tier = data.Sku?.Tier,
                Family = data.Sku?.Family
            },
            Kind = data.Kind,
            Properties = data.Properties.ToObjectFromJson<StorageAccountResourceProperties>(GlobalSettings.JsonOptions)
        };
    }
}