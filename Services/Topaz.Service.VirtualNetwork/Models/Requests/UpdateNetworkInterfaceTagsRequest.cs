namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record UpdateNetworkInterfaceTagsRequest
{
    public IDictionary<string, string>? Tags { get; init; }
}
