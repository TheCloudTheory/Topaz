using Azure;
using Azure.Core;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.AspNetCore.Extensions;

internal static class TopazConfigurationExtensions
{
    public static TopazEnvironmentBuilder AddTopaz(this IConfigurationBuilder builder, Guid defaultSubscriptionId)
    {
        return new TopazEnvironmentBuilder(defaultSubscriptionId);
    }

    public static async Task<TopazEnvironmentBuilder> AddSubscription(this TopazEnvironmentBuilder builder,
        Guid subscriptionId, string subscriptionName)
    {
        using var topaz = new TopazArmClient();
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        
        return builder;
    }

    public static async Task<TopazEnvironmentBuilder> AddResourceGroup(this Task<TopazEnvironmentBuilder> builder, Guid subscriptionId, string resourceGroupName)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroups = subscription.GetResourceGroups();
        
        _ = await resourceGroups.CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(AzureLocation.WestEurope));

        return concrete;
    }

    public static async Task<TopazEnvironmentBuilder> AddKeyVault(this Task<TopazEnvironmentBuilder> builder, string resourceGroupName, string keyVaultName)
    {
        var concrete = await builder;
        var subscription = await concrete.ArmClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        
        _ = await resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdateAsync(WaitUntil.Completed, keyVaultName, operation, CancellationToken.None);
        
        return concrete;
    }

    public static async Task<TopazEnvironmentBuilder> AddKeyVault(this Task<TopazEnvironmentBuilder> builder, string resourceGroupName, string keyVaultName,
        IDictionary<string, string> secrets)
    {
        await builder.AddKeyVault(resourceGroupName, keyVaultName);
        
        var concrete = await builder;
        var credentials = new AzureLocalCredential();
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName),
            credential: credentials, new SecretClientOptions
            {
                DisableChallengeResourceVerification = true
            });

        foreach (var secret in secrets)
        {
            await client.SetSecretAsync(secret.Key, secret.Value);
        }

        return concrete;
    }
}