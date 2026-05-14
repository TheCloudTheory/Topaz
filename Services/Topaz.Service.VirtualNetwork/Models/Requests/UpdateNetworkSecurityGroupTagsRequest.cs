namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record UpdateNetworkSecurityGroupTagsRequest
{
    public IDictionary<string, string>? Tags { get; init; }
}
