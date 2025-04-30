using System.Text.Json;

namespace Azure.Local.Service.Storage.Models;

public class StorageAccount(string Name)
{
    public string Name { get; init; } = Name;
    public EndpointsMetadata Endpoints { get; init; } = new EndpointsMetadata(Name);

    public class EndpointsMetadata(string storageAccountName)
    {
        public string TableEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/table";
        public string BlobEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/blob";
        public string QueueEndpoint { get; init; } = $"http://localhost:8899/storage/{storageAccountName}/queue";
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
