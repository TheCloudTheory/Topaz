using System.Text.Json.Serialization;

namespace Topaz.Service.ResourceGroup.Models.Requests;

internal record CreateOrUpdateResourceGroupRequest
{
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}
