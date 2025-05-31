using Azure;

namespace Topaz.Service.Storage.Models;

// TODO: Add additional properties as listed in https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties?tabs=microsoft-entra-id
internal sealed class BlobProperties
{
    public string Name { get; init; } = null!;
    public DateTimeOffset DateUploaded { get; init; }
    public string BlobType => "BlockBlob";
    public ETag ETag { get; init; }
    public DateTimeOffset? LastModified { get; set; }
}