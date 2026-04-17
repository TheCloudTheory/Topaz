using Azure;

namespace Topaz.Service.Storage.Models;

// See https://learn.microsoft.com/en-us/rest/api/storageservices/get-blob-properties?tabs=microsoft-entra-id#response-headers
public sealed class BlobProperties
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public BlobProperties()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public BlobProperties(
        DateTimeOffset dateUploaded,
        DateTimeOffset lastModified)
    {
        // We're using this specific constructor to encapsulate the logic
        // behind parsing date and time for the blob properties. 
        // In Blob Storage, those properties are supposed to follow RFC-1123,
        // which is basically the extension of RFC-882.
        // See https://www.rfc-editor.org/rfc/rfc1123#page-55 for reference.
        DateUploaded = dateUploaded.ToString("R");
        LastModified = lastModified.ToString("R");
    }
    
    public string Name { get; init; } = null!;
    public string DateUploaded { get; init; }
    public string BlobType { get; init; } = "BlockBlob";
    public ETag ETag { get; set; }
    public string? LastModified { get; set; }
    public long ContentLength { get; init; }
    public string ContentType { get; set; } = "application/octet-stream";
    public string? ContentEncoding { get; set; }
    public string? ContentLanguage { get; set; }
    public string? CacheControl { get; set; }
    public string? ContentDisposition { get; set; }
    public string? CopyId { get; set; }
    public List<BlobPageRange> PageRanges { get; set; } = [];
}
