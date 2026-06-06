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
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/deployments/{deploymentName}/operations"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var deploymentName = path.ExtractValueFromPath(6);

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

        // Return an empty operations list — Topaz does not track per-resource operations.
        // An empty list tells the cmdlet the deployment exists; it then uses provisioningState
        // on the deployment itself to determine whether it is still running.
        response.CreateJsonContentResponse(new DeploymentOperationsListResult([]));
    }

    private sealed class DeploymentOperationsListResult(object[] value)
    {
        public object[] Value { get; } = value;

        public override string ToString() =>
            System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
