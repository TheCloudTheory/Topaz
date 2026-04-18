using System.Text.Json;
using System.Text.Json.Serialization;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class GetKeyVersionsResponse
{
    [JsonPropertyName("value")]
    public KeyVersionItem[]? Value { get; init; }

    [JsonPropertyName("nextLink")]
    public string NextLink { get; init; } = string.Empty;

    public class KeyVersionItem
    {
        [JsonPropertyName("kid")]
        public string? Kid { get; init; }

        [JsonPropertyName("attributes")]
        public KeyVersionAttributes? Attributes { get; init; }

        public class KeyVersionAttributes
        {
            [JsonPropertyName("enabled")]
            public bool Enabled { get; init; }

            [JsonPropertyName("created")]
            public long Created { get; init; }

            [JsonPropertyName("updated")]
            public long Updated { get; init; }

            [JsonPropertyName("recoveryLevel")]
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
