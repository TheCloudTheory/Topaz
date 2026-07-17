using System.Collections.Concurrent;

namespace Topaz.Service.Disk;

internal sealed class DiskAccessLroStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, DiskAccessLroEntry> _entries = new();

    public static DiskAccessLroStore Instance { get; } = new();

    public void Add(Guid operationId, string accessSas) =>
        _entries[operationId] = new DiskAccessLroEntry(accessSas, DateTimeOffset.UtcNow);

    public DiskAccessLroEntry? TryGet(Guid operationId)
    {
        PurgeStale();
        return _entries.GetValueOrDefault(operationId);
    }

    public void Remove(Guid operationId) => _entries.TryRemove(operationId, out _);

    private void PurgeStale()
    {
        var cutoff = DateTimeOffset.UtcNow - Ttl;
        foreach (var key in _entries.Keys)
            if (_entries.TryGetValue(key, out var e) && e.CreatedAt < cutoff)
                _entries.TryRemove(key, out _);
    }
}

internal sealed record DiskAccessLroEntry(string AccessSAS, DateTimeOffset CreatedAt);
