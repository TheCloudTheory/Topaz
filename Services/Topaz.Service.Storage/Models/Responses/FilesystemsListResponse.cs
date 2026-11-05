using Azure;
using JetBrains.Annotations;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Models.Responses;

[UsedImplicitly]
internal sealed class FilesystemsListResponse : TopazApiModel
{
    public FilesystemResponse[]? Filesystems { get; set; }

    public static FilesystemsListResponse From(Filesystem[] filesystems, int? maxResults, string? prefix)
    {
        if (maxResults.HasValue)
        {
            filesystems = [.. filesystems.Take(maxResults.Value)];
        }
        
        if(!string.IsNullOrWhiteSpace(prefix))
        {
            filesystems = [.. filesystems.Where(x => x.Name!.StartsWith(prefix))];
        }
        
        return new FilesystemsListResponse
        {
            Filesystems = [.. filesystems.Select(FilesystemResponse.From)]
        };
    }
}

internal class FilesystemResponse
{
    public static FilesystemResponse From(Filesystem filesystem)
    {
        return new FilesystemResponse
        {
            ETag = filesystem.ETag,
            LastModified = filesystem.LastModified,
            Name = filesystem.Name
        };
    }

    public string? Name { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public ETag? ETag { get; init; }
}