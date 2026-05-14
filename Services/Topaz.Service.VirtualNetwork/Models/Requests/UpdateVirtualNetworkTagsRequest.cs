using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

[UsedImplicitly]
public sealed class UpdateVirtualNetworkTagsRequest
{
    public IDictionary<string, string>? Tags { get; init; }
}
