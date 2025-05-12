using System.Text.Json.Serialization;

namespace Topaz.Service.KeyVault.Models;

internal record class CreateOrUpdateRequest
{
    [JsonPropertyName("location")]
    public string? Location { get; set; }
}
