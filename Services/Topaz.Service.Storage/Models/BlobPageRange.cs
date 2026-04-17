namespace Topaz.Service.Storage.Models;

public sealed class BlobPageRange
{
    public BlobPageRange()
    {
    }

    public BlobPageRange(long start, long end)
    {
        Start = start;
        End = end;
    }

    public long Start { get; init; }
    public long End { get; init; }
}
