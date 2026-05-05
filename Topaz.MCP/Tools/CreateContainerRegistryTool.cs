using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages Azure Container Registry resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateContainerRegistryTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Container Registry in the given resource group and returns its login server and credentials.")]
    [UsedImplicitly]
    public static async Task<ContainerRegistryResult> CreateContainerRegistry(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group.")]
        string resourceGroupName,
        [Description("Name of the registry to create (5–50 alphanumeric characters).")]
        string registryName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("SKU name: Basic, Standard, or Premium (default: Basic).")]
        string sku = "Basic",
        [Description("Whether to enable the admin user account (default: true).")]
        bool adminUserEnabled = true)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var skuName = sku switch
        {
            "Standard" => ContainerRegistrySkuName.Standard,
            "Premium"  => ContainerRegistrySkuName.Premium,
            _          => ContainerRegistrySkuName.Basic,
        };

        var data = new ContainerRegistryData(new AzureLocation(location), new ContainerRegistrySku(skuName))
        {
            IsAdminUserEnabled = adminUserEnabled,
        };

        var registry = await resourceGroup.Value.GetContainerRegistries()
            .CreateOrUpdateAsync(WaitUntil.Completed, registryName, data)
            .ConfigureAwait(false);

        var loginServer = TopazResourceHelpers.GetContainerRegistryLoginServer(registryName);

        if (!adminUserEnabled)
        {
            return new ContainerRegistryResult
            {
                RegistryName = registryName,
                LoginServer = loginServer,
                AdminUsername = null,
                AdminPassword = null,
            };
        }

        var credentialsResult = await registry.Value.GetCredentialsAsync().ConfigureAwait(false);
        var adminUsername = credentialsResult.Value.Username;
        var adminPassword = credentialsResult.Value.Passwords?.FirstOrDefault()?.Value;

        return new ContainerRegistryResult
        {
            RegistryName = registryName,
            LoginServer = loginServer,
            AdminUsername = adminUsername,
            AdminPassword = adminPassword,
        };
    }

    public sealed record ContainerRegistryResult
    {
        public required string RegistryName { [UsedImplicitly] get; init; }
        public required string LoginServer { [UsedImplicitly] get; init; }
        public required string? AdminUsername { [UsedImplicitly] get; init; }
        public required string? AdminPassword { [UsedImplicitly] get; init; }
    }
}
