using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Returns the list of operations for a resource-group-scope deployment.
/// Azure PowerShell's Stop-AzResourceGroupDeployment calls this endpoint to check
/// whether the deployment is still running before issuing the cancel request.
/// Topaz returns an empty list for completed deployments and 404 if the deployment
/// does not exist.
/// </summary>
public sealed class GetDeploymentOperationsEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        // Legacy path used by Azure PowerShell
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/deployments/{deploymentName}/operations",
        // Path used by the Azure SDK and Azure CLI
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/operations"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        // Legacy path: .../deployments/{name}/operations  → name at index 6
        // SDK path:    .../providers/Microsoft.Resources/deployments/{name}/operations → name at index 8
        var deploymentName = path.Contains("/providers/Microsoft.Resources/", StringComparison.OrdinalIgnoreCase)
            ? path.ExtractValueFromPath(8)
            : path.ExtractValueFromPath(6);

        logger.LogDebug(nameof(GetDeploymentOperationsEndpoint), nameof(GetResponse),
            "Get deployment operations: subscription `{0}`, resource group `{1}`, deployment `{2}`",
            subscriptionIdentifier, resourceGroupIdentifier, deploymentName);

        var deploymentOp = _controlPlane.GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName!);
        if (deploymentOp.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = Deployment.OperationStore.GetRgScopeDirectory(
            subscriptionIdentifier.Value.ToString(), resourceGroupIdentifier.Value, deploymentName!);
        var operations = Deployment.OperationStore.GetAll(dir);
        response.CreateJsonContentResponse(new DeploymentOperationsListResult(operations));
    }

    private sealed class DeploymentOperationsListResult(IReadOnlyList<Models.OperationRecord> value)
    {
        public IReadOnlyList<Models.OperationRecord> Value { get; } = value;

        public override string ToString() =>
            System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
