using System.Collections.Concurrent;

namespace Topaz.Service.Disk;

/// <summary>
/// Holds byte-addressable storage for disks that have an active SAS grant.
/// Small disks (≤ InMemoryThresholdBytes) are kept in memory; larger ones
/// are backed by a sparse file on disk.
/// </summary>
internal sealed class DiskByteStore
{
    private const long InMemoryThresholdBytes = 4L * 1024 * 1024 * 1024; // 4 GB

    private static readonly string VhdDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".topaz", "disks");

    private readonly ConcurrentDictionary<Guid, IDiskStore> _stores = new();

    public static DiskByteStore Instance { get; } = new();

    public void Allocate(Guid uniqueId, long sizeBytes)
    {
        IDiskStore store = sizeBytes <= InMemoryThresholdBytes
            ? new InMemoryDiskStore(sizeBytes)
            : new FileDiskStore(Path.Combine(VhdDirectory, $"{uniqueId}.vhd"), sizeBytes);

        _stores[uniqueId] = store;
    }

    public IDiskStore? TryGet(Guid uniqueId) =>
        _stores.TryGetValue(uniqueId, out var store) ? store : null;

    public void Release(Guid uniqueId)
    {
        if (_stores.TryRemove(uniqueId, out var store))
            store.Dispose();
    }
}

internal interface IDiskStore : IDisposable
{
    long Size { get; }
    void Write(long offset, ReadOnlySpan<byte> data);
    int Read(long offset, Span<byte> buffer);
}

internal sealed class InMemoryDiskStore(long size) : IDiskStore
{
    private readonly byte[] _data = new byte[size];

    public long Size => _data.LongLength;

    public void Write(long offset, ReadOnlySpan<byte> data) =>
        data.CopyTo(_data.AsSpan((int)offset));

    public int Read(long offset, Span<byte> buffer)
    {
        var available = (int)Math.Min(buffer.Length, _data.LongLength - offset);
        _data.AsSpan((int)offset, available).CopyTo(buffer);
        return available;
    }

    public void Dispose() { /* GC handles the array */ }
}

internal sealed class FileDiskStore : IDiskStore
{
    private readonly FileStream _stream;

    public FileDiskStore(string path, long size)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite,
            FileShare.ReadWrite, bufferSize: 4096, FileOptions.RandomAccess);
        _stream.SetLength(size); // creates sparse file on most OSes
        Size = size;
    }

    public long Size { get; }

    public void Write(long offset, ReadOnlySpan<byte> data)
    {
        lock (_stream)
        {
            _stream.Position = offset;
            _stream.Write(data);
        }
    }

    public int Read(long offset, Span<byte> buffer)
    {
        lock (_stream)
        {
            _stream.Position = offset;
            return _stream.Read(buffer);
        }
    }

    public void Dispose()
    {
        var path = _stream.Name;
        _stream.Dispose();
        try { File.Delete(path); }
        catch (IOException) { /* best-effort */ }
    }
}
