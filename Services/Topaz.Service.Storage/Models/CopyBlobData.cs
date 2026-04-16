namespace Topaz.Service.Storage.Models;

internal sealed record CopyBlobData(BlobProperties Properties, string CopyId);
