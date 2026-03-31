using System.Text.Json;
using Topaz.Shared;

namespace Topaz.Service.KeyVault.Models.Responses;

internal sealed class VaultAccessPolicyParametersResponse
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public VaultAccessPolicyPropertiesResponse? Properties { get; init; }

    public override string ToString() => JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);

    internal sealed class VaultAccessPolicyPropertiesResponse
    {
        public KeyVaultResourceProperties.AccessPolicyEntry[]? AccessPolicies { get; init; }
    }
}
