using System.Text.Json.Serialization;
using Azure.ResourceManager.Resources;

namespace Topaz.Service.ResourceGroup.Models.Requests;

public record CreateOrUpdateResourceGroupRequest
{
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

    public static CreateOrUpdateResourceGroupRequest From(ResourceGroupData rgData)
    {
        return new CreateOrUpdateResourceGroupRequest
        {
            Location = rgData.Location,
            Tags = rgData.Tags
        };
    }
}
