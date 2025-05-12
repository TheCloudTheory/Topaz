using System.Text.Json;

namespace Topaz.Service.Storage.Models;

public class StorageAccount(string Name, string ResourceGroup, string Location, string SubscriptionId)
{
    public string Id => $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroup}/storageAccounts/{Name}";
    public string Name { get; set; } = Name;
    public string ResourceGroup { get; set; } = ResourceGroup;
    public string Location { get; set; } = Location;
    public string SubscriptionId { get; } = SubscriptionId;
    public EndpointsMetadata Endpoints { get; init; } = new EndpointsMetadata(Name);

    public class EndpointsMetadata(string storageAccountName)
    {
        public string TableEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/";
        public string BlobEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/";
        public string QueueEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/";
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
