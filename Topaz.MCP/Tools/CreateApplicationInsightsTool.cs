using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.ApplicationInsights.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure Application Insights component resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateApplicationInsightsTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates an Application Insights component in the given resource group.")]
    [UsedImplicitly]
    public static async Task<ApplicationInsightsResult> CreateApplicationInsights(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the component will be created.")]
        string resourceGroupName,
        [Description("Name of the Application Insights component to create.")]
        string componentName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Application type. Defaults to 'web'.")]
        string applicationType = "web",
        [Description("Optional resource ID of a Log Analytics workspace to link to this component.")]
        string? workspaceResourceId = null)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var data = new ApplicationInsightsComponentData(new AzureLocation(location), "web")
        {
            ApplicationType = new ApplicationInsightsApplicationType(applicationType),
            FlowType = ComponentFlowType.Bluefield,
            RequestSource = ComponentRequestSource.Rest,
        };

        if (workspaceResourceId is not null)
        {
            data.WorkspaceResourceId = new ResourceIdentifier(workspaceResourceId);
        }

        var operation = await resourceGroup.Value.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, data)
            .ConfigureAwait(false);

        var component = operation.Value;

        return new ApplicationInsightsResult
        {
            Name = component.Data.Name,
            ResourceId = component.Data.Id?.ToString(),
            InstrumentationKey = component.Data.InstrumentationKey,
            ConnectionString = component.Data.ConnectionString,
            Location = component.Data.Location.ToString(),
            ProvisioningState = component.Data.ProvisioningState,
        };
    }

    public sealed record ApplicationInsightsResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string? InstrumentationKey { [UsedImplicitly] get; init; }
        public required string? ConnectionString { [UsedImplicitly] get; init; }
        public required string? Location { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
}
