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
[Description("Creates Azure App Service Site (Web App / Function App) resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateAppServiceSiteTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Web App or Function App (Microsoft.Web/sites) in the given resource group.")]
    [UsedImplicitly]
    public static async Task<AppServiceSiteResult> CreateAppServiceSite(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the site will be created.")]
        string resourceGroupName,
        [Description("Name of the site to create.")]
        string siteName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Kind of site: 'app' (Web App), 'functionapp', or 'functionapp,linux'. Defaults to 'app'.")]
        string kind = "app",
        [Description("Resource ID of the App Service Plan (serverfarm) to associate with this site. Optional.")]
        string? serverFarmId = null)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var content = new WebSiteData(new AzureLocation(location))
        {
            Kind = kind,
        };

        if (!string.IsNullOrWhiteSpace(serverFarmId))
            content.AppServicePlanId = new ResourceIdentifier(serverFarmId);

        var operation = await resourceGroup.Value.GetWebSites()
            .CreateOrUpdateAsync(WaitUntil.Completed, siteName, content)
            .ConfigureAwait(false);

        return new AppServiceSiteResult
        {
            Name = operation.Value.Data.Name,
            ResourceId = operation.Value.Data.Id?.ToString(),
            DefaultHostName = operation.Value.Data.DefaultHostName,
            State = operation.Value.Data.State,
            Kind = operation.Value.Data.Kind,
        };
    }

    public sealed record AppServiceSiteResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string? DefaultHostName { [UsedImplicitly] get; init; }
        public required string? State { [UsedImplicitly] get; init; }
        public required string? Kind { [UsedImplicitly] get; init; }
    }
}
