namespace Topaz.Service.KeyVault.Models.Requests;

public record class UpdateKeyRequest
{
    public string[]? KeyOps { get; init; }
    public UpdateKeyAttributes? Attributes { get; init; }
    public Dictionary<string, string>? Tags { get; init; }

    public record class UpdateKeyAttributes
    {
        public bool? Enabled { get; init; }
    }
}
