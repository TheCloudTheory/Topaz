namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record UpdatePublicIpAddressTagsRequest
{
    public IDictionary<string, string>? Tags { get; init; }
}
