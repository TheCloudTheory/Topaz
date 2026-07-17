using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates Azure Log Analytics workspace resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateLogAnalyticsTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a Log Analytics workspace in the given resource group.")]
    [UsedImplicitly]
    public static async Task<LogAnalyticsWorkspaceResult> CreateLogAnalyticsWorkspace(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group where the workspace will be created.")]
        string resourceGroupName,
        [Description("Name of the Log Analytics workspace to create.")]
        string workspaceName,
        [Description("Azure location (e.g. 'westeurope').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId,
        [Description("Retention period in days. Defaults to 30.")]
        int retentionInDays = 30)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        var data = new OperationalInsightsWorkspaceData(new AzureLocation(location))
        {
            RetentionInDays = retentionInDays,
            Sku = new OperationalInsightsWorkspaceSku(OperationalInsightsWorkspaceSkuName.PerGB2018),
        };

        var operation = await resourceGroup.Value.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, data)
            .ConfigureAwait(false);

        var workspace = operation.Value;

        return new LogAnalyticsWorkspaceResult
        {
            Name = workspace.Data.Name,
            ResourceId = workspace.Data.Id?.ToString(),
            CustomerId = workspace.Data.CustomerId?.ToString(),
            Location = workspace.Data.Location.ToString(),
            RetentionInDays = workspace.Data.RetentionInDays,
            ProvisioningState = workspace.Data.ProvisioningState?.ToString(),
        };
    }

    public sealed record LogAnalyticsWorkspaceResult
    {
        public required string? Name { [UsedImplicitly] get; init; }
        public required string? ResourceId { [UsedImplicitly] get; init; }
        public required string? CustomerId { [UsedImplicitly] get; init; }
        public required string? Location { [UsedImplicitly] get; init; }
        public required int? RetentionInDays { [UsedImplicitly] get; init; }
        public required string? ProvisioningState { [UsedImplicitly] get; init; }
    }
}
