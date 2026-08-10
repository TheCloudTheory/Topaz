using System.Text.Json;
using Azure.ResourceManager.Resources;
using Topaz.Shared;

namespace Topaz.Service.VirtualNetwork.Models.Requests;

internal record CreateOrUpdateNetworkSecurityGroupRequest
{
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public CreateOrUpdateNetworkSecurityGroupRequestProperties? Properties { get; init; }

    internal class CreateOrUpdateNetworkSecurityGroupRequestProperties
    {
        public JsonElement? SecurityRules { get; init; }
    }

    public static CreateOrUpdateNetworkSecurityGroupRequest From(GenericResourceData data)
    {
        return new CreateOrUpdateNetworkSecurityGroupRequest
        {
            Location = data.Location,
            Tags = data.Tags,
            Properties =
                data.Properties.ToObjectFromJson<CreateOrUpdateNetworkSecurityGroupRequestProperties>(GlobalSettings
                    .JsonOptions)
        };
    }
}
