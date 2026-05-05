using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Secrets;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure Key Vault resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateKeyVaultTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Key Vault in the given resource group and optionally seeds it with a secret.")]
    [UsedImplicitly]
    public static async Task<KeyVaultResult> CreateKeyVault(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the Key Vault will be created.")]
        string resourceGroupName,
        [Description("Name of the Key Vault to create.")]
        string keyVaultName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Optional name of an initial secret to seed into the vault.")]
        string? secretName = null,
        [Description("Optional value of the initial secret. Required when secretName is provided.")]
        string? secretValue = null)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var properties = new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard));
        var content = new KeyVaultCreateOrUpdateContent(new AzureLocation(location), properties);

        await resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdateAsync(WaitUntil.Completed, keyVaultName, content)
            .ConfigureAwait(false);

        if (secretName is null || secretValue is null)
        {
            return new KeyVaultResult
            {
                Name = keyVaultName,
                VaultUri = TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName).ToString(),
                SeededSecret = secretName,
            };
        }
            
        var secretClient = new SecretClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
            credential: credentials,
            new SecretClientOptions { DisableChallengeResourceVerification = true });

        await secretClient.SetSecretAsync(secretName, secretValue).ConfigureAwait(false);

        return new KeyVaultResult
        {
            Name = keyVaultName,
            VaultUri = TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName).ToString(),
            SeededSecret = secretName,
        };
    }

    public sealed record KeyVaultResult
    {
        public required string Name { [UsedImplicitly] get; init; }
        public required string VaultUri { [UsedImplicitly] get; init; }
        public required string? SeededSecret { [UsedImplicitly] get; init; }
    }
}
