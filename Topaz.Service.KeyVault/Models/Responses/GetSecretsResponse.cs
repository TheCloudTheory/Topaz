namespace Topaz.Service.KeyVault.Models.Responses;

public class GetSecretsResponse
{
    public Secret[]? Value { get; init; }
    public string NextLink { get; init; } = string.Empty;

    public class Secret
    {
        public string? ContentType { get; init; }
        public string? Id { get; init; }
        public SecretAttributes? Attributes { get; init; }

        public class SecretAttributes
        {
            public bool Enabled { get; init; }
            public long Created { get; init; }
            public long Updated { get; init; }
        }
    }
}