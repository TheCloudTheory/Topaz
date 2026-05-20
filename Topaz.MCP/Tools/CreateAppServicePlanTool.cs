using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure App Service Plan resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateAppServicePlanTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates an App Service Plan (serverfarm) in the given resource group.")]
    [UsedImplicitly]
    public static async Task<AppServicePlanResult> CreateAppServicePlan(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the App Service Plan will be created.")]
        string resourceGroupName,
        [Description("Name of the App Service Plan to create.")]
        string planName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("SKU name for the plan (e.g. 'B1', 'S1', 'P1v3'). Defaults to 'B1'.")]
        string skuName = "B1",
        [Description("SKU tier (e.g. 'Basic', 'Standard', 'PremiumV3'). Defaults to 'Basic'.")]
        string skuTier = "Basic")
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var sku = new AppServiceSkuDescription { Name = skuName, Tier = skuTier };
        var content = new AppServicePlanData(new AzureLocation(location)) { Sku = sku };

        var operation = await resourceGroup.Value.GetAppServicePlans()
            .CreateOrUpdateAsync(WaitUntil.Completed, planName, content)
            .ConfigureAwait(false);

        return new AppServicePlanResult
        {
            Name = operation.Value.Data.Name,
            ResourceId = operation.Value.Data.Id?.ToString(),
            SkuName = operation.Value.Data.Sku?.Name,
            ProvisioningState = operation.Value.Data.ProvisioningState?.ToString(),
        };
    }

    public sealed record AppServicePlanResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string? SkuName { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
}
