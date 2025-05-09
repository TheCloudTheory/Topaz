using System.Text.Json;

namespace Azure.Local.Service.Storage.Models;

public class StorageAccount(string Name, string ResourceGroup, string Location)
{
    public string Name { get; init; } = Name;
    public string ResourceGroup {get;init;} = ResourceGroup;
    public string Location {get;init;} = Location;
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
