using Topaz.ResourceManager;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdatePublicIpAddressRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public ResourceSku? Sku { get; init; }
    public CreateOrUpdatePublicIpAddressRequestProperties? Properties { get; init; }

    internal class CreateOrUpdatePublicIpAddressRequestProperties
    {
        public string? PublicIPAllocationMethod { get; init; }
        public string? PublicIPAddressVersion { get; init; }
        public int? IdleTimeoutInMinutes { get; init; }
    }
}
