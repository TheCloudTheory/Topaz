using Azure.ResourceManager.Resources;
using Topaz.ResourceManager;
using Topaz.Shared;

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

    public static CreateOrUpdatePublicIpAddressRequest From(GenericResourceData data)
    {
        return new CreateOrUpdatePublicIpAddressRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Sku = new ResourceSku
            {
                Name = data.Sku.Name,
                Capacity = data.Sku.Capacity,
                Family = data.Sku.Family,
                Tier = data.Sku.Tier
            },
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdatePublicIpAddressRequestProperties>(GlobalSettings
                    .JsonOptions)
        };
    }
}
