using System.ComponentModel;
using Azure;
using Azure.ResourceManager;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Deletes Azure resources in a running Topaz instance.")]
[UsedImplicitly]
public sealed class DeleteResourcesTool
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    [McpServerTool]
    [Description("Deletes a resource group and all resources it contains.")]
    [UsedImplicitly]
    public static async Task<DeleteResult> DeleteResourceGroup(
        [Description("ID of the subscription containing the resource group.")]
        Guid subscriptionId,
        [Description("Name of the resource group to delete.")]
        string resourceGroupName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync().ConfigureAwait(false);
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName).ConfigureAwait(false);

        await resourceGroup.Value.DeleteAsync(WaitUntil.Completed).ConfigureAwait(false);

        return new DeleteResult
        {
            ResourceType = "ResourceGroup",
            Name = resourceGroupName,
            Deleted = true,
        };
    }

    public sealed record DeleteResult
    {
        public required string ResourceType { [UsedImplicitly] get; init; }
        public required string Name { [UsedImplicitly] get; init; }
        public required bool Deleted { [UsedImplicitly] get; init; }
    }
}
