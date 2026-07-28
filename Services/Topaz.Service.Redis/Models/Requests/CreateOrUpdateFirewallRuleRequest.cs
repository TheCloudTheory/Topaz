using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Topaz.Service.Redis.Models.Requests;

internal sealed class CreateOrUpdateFirewallRuleRequest
{
    public CreateOrUpdateFirewallRuleRequestProperties? Properties { get; set; }

    [UsedImplicitly]
    internal class CreateOrUpdateFirewallRuleRequestProperties
    {
        [JsonPropertyName("startIP")]
        public string? StartIp { get; set; }
        
        [JsonPropertyName("endIP")]
        public string? EndIp { get; set; }
    }
}