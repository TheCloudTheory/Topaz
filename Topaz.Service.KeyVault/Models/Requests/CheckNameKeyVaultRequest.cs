namespace Topaz.Service.KeyVault.Models.Requests;

public record CheckNameKeyVaultRequest
{
    public required string Name  { get; init; }
    public required string Type  { get; init; }
}