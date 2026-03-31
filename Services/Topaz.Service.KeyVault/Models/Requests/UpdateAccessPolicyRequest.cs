using Topaz.Service.KeyVault.Models;

namespace Topaz.Service.KeyVault.Models.Requests;

internal record UpdateAccessPolicyRequest
{
    public VaultAccessPolicyProperties? Properties { get; init; }

    internal class VaultAccessPolicyProperties
    {
        public KeyVaultResourceProperties.AccessPolicyEntry[]? AccessPolicies { get; set; }
    }
}
