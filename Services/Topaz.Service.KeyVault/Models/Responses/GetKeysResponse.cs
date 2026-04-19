using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class GetKeysResponse
{
    public KeyItem[]? Value { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public class KeyItem
    {
        public string? Kid { get; init; }
        public KeyItemAttributes? Attributes { get; init; }
        public Dictionary<string, string>? Tags { get; init; }

        public class KeyItemAttributes
        {
            public bool Enabled { get; init; }
            public long Created { get; init; }
            public long Updated { get; init; }
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public override string ToString() =>
        JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
}
