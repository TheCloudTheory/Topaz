using System.Text.Json;

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
}
