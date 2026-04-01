namespace Topaz.Service.KeyVault.Models.Requests;

public record class UpdateSecretRequest
{
    public string? ContentType { get; init; }
    public UpdateSecretAttributes? Attributes { get; init; }

    public record class UpdateSecretAttributes
    {
        public bool? Enabled { get; init; }
    }
}
