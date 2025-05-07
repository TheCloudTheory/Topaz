using System.Text.Json.Serialization;

namespace Azure.Local.Service.ResourceGroup.Models;

internal record class CreateOrUpdateRequest
{
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}
