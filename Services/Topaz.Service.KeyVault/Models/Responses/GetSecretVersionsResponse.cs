using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

public class GetSecretVersionsResponse
{
    public SecretVersionItem[]? Value { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public class SecretVersionItem
    {
        public string? ContentType { get; init; }
        public string? Id { get; init; }
        public SecretVersionAttributes? Attributes { get; init; }

        public class SecretVersionAttributes
        {
            public bool Enabled { get; init; }
            public long Created { get; init; }
            public long Updated { get; init; }
            public string RecoveryLevel { get; init; } = "Recoverable+Purgeable";
        }
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
