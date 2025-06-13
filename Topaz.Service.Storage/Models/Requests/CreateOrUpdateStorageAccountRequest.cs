using Azure.ResourceManager.Models;
using Topaz.ResourceManager;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Models.Requests;

internal record CreateOrUpdateStorageAccountRequest
{
    public ResourceSku? Sku { get; init; }
    public string? Kind { get; init; }
    public string? Location { get; set; }
    public IDictionary<string, string>? Tags { get; init; }
    public ManagedServiceIdentity? Identity { get; init; }
    public StorageAccountProperties? Properties { get; init; }
}