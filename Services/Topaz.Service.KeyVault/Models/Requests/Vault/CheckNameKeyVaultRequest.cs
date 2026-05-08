namespace Topaz.Service.KeyVault.Models.Requests.Vault;

public record CheckNameKeyVaultRequest
{
    public required string Name  { get; init; }
    public string? Type  { get; init; }
}