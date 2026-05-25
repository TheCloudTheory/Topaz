using System.ComponentModel;
using JetBrains.Annotations;
using ModelContextProtocol.Server;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.MCP.Tools;

[McpServerToolType]
[Description("Creates and manages tenant-scope ARM template deployments in a running Topaz instance.")]
[UsedImplicitly]
public sealed class DeployTemplateTool
{
    [McpServerTool]
    [Description("Creates or updates a tenant-scope ARM template deployment.")]
    [UsedImplicitly]
    public static async Task<TenantDeploymentResult> CreateOrUpdateTenantDeployment(
        [Description("Name for the deployment.")]
        string deploymentName,
        [Description("Azure location for the deployment (e.g. 'westeurope', 'eastus').")]
        string location,
        [Description("ARM template JSON string to deploy.")]
        string templateJson,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        using var topaz = new TopazArmClient(credentials);

        var result = await topaz.CreateDeploymentAtTenantScopeAsync(
            deploymentName, location, templateJson).ConfigureAwait(false);

        // Poll until the background orchestrator finishes (analogous to WaitUntil.Completed).
        var provisioningState = result["properties"]?["provisioningState"]?.GetValue<string>() ?? "Created";
        while (provisioningState == "Created" || provisioningState == "Running")
        {
            await Task.Delay(200).ConfigureAwait(false);
            result = await topaz.GetDeploymentAtTenantScopeAsync(deploymentName).ConfigureAwait(false);
            provisioningState = result["properties"]?["provisioningState"]?.GetValue<string>() ?? "Created";
        }

        return new TenantDeploymentResult
        {
            Name = result["name"]!.GetValue<string>(),
            Id = result["id"]!.GetValue<string>(),
            ProvisioningState = provisioningState
        };
    }

    [McpServerTool]
    [Description("Gets a tenant-scope ARM template deployment by name.")]
    [UsedImplicitly]
    public static async Task<TenantDeploymentResult> GetTenantDeployment(
        [Description("Name of the deployment to retrieve.")]
        string deploymentName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        using var topaz = new TopazArmClient(credentials);

        var result = await topaz.GetDeploymentAtTenantScopeAsync(deploymentName).ConfigureAwait(false);

        return new TenantDeploymentResult
        {
            Name = result["name"]!.GetValue<string>(),
            Id = result["id"]!.GetValue<string>(),
            ProvisioningState = result["properties"]?["provisioningState"]?.GetValue<string>() ?? "Unknown"
        };
    }

    [McpServerTool]
    [Description("Deletes a tenant-scope ARM template deployment by name.")]
    [UsedImplicitly]
    public static async Task DeleteTenantDeployment(
        [Description("Name of the deployment to delete.")]
        string deploymentName,
        [Description("Object ID of the user performing the operation. Use empty GUID for superadmin.")]
        string objectId)
    {
        var credentials = new AzureLocalCredential(objectId);
        using var topaz = new TopazArmClient(credentials);

        var response = await topaz.DeleteDeploymentAtTenantScopeAsync(deploymentName).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public sealed record TenantDeploymentResult
    {
        public required string Name { [UsedImplicitly] get; init; }
        public required string Id { [UsedImplicitly] get; init; }
        public required string ProvisioningState { [UsedImplicitly] get; init; }
    }
}
