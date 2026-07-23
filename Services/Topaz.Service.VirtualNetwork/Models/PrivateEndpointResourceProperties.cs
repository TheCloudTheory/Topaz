using System.Text.Json;
using JetBrains.Annotations;

namespace Topaz.Service.VirtualNetwork.Models;

public sealed class PrivateEndpointResourceProperties
{
    public JsonElement? Subnet { get; set; }
    public JsonElement? PrivateLinkServiceConnections { get; set; }
    public JsonElement? NetworkInterfaces { get; set; }
    public JsonElement? CustomDnsConfigs { get; set; }
    [UsedImplicitly] public string ProvisioningState => "Succeeded";
}
