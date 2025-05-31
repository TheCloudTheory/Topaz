namespace Topaz.Service.Storage.Models;

// TODO: Add additional properties as listed in https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties?tabs=microsoft-entra-id
internal sealed class BlobProperties
{
    public string Name { get; set; } = null!;
}