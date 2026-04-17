namespace Topaz.Service.Storage.Models;

internal sealed class BlobPageRangesResult
{
    public BlobProperties BlobProperties { get; init; } = null!;
    public IReadOnlyCollection<BlobPageRange> PageRanges { get; init; } = [];
}
