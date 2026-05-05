using System.ComponentModel;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages Azure resource groups in a running Topaz instance.")]
[UsedImplicitly]
public sealed class CreateResourceGroupTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Creates a resource group in the given subscription.")]
    [UsedImplicitly]
    public static async Task<ResourceGroupResult> CreateResourceGroup(
        [Description("ID of the subscription where the resource group will be created.")]
        Guid subscriptionId,
        [Description("Name of the resource group to create.")]
        string resourceGroupName,
        [Description("Azure location for the resource group (e.g. 'westeurope', 'eastus').")]
        string location,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);

        var result = await subscription.GetResourceGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName, new ResourceGroupData(new AzureLocation(location)))
            .ConfigureAwait(false);

        return new ResourceGroupResult
        {
            Name = result.Value.Data.Name,
            Location = result.Value.Data.Location.ToString(),
            SubscriptionId = subscriptionId.ToString(),
        };
    }

    public sealed record ResourceGroupResult
    {
        public required string Name { [UsedImplicitly] get; init; }
        public required string Location { [UsedImplicitly] get; init; }
        public required string SubscriptionId { [UsedImplicitly] get; init; }
    }
}
