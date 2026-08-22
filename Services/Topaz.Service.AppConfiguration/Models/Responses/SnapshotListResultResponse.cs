using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Responses;

internal sealed class SnapshotListResultResponse : TopazApiModel
{
    public SnapshotSubresourceProperties[] Items { get; set; } = [];

    public static SnapshotListResultResponse From(SnapshotFullSubresource[] snapshots)
    {
        return new SnapshotListResultResponse
        {
            Items = snapshots.Select(s => s.Properties).ToArray(),
        };
    }
}