using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationByIdEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/operations/{operationId}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var deploymentName = path.ExtractValueFromPath(8);
        var operationId = path.ExtractValueFromPath(10);

        logger.LogDebug(nameof(GetDeploymentOperationByIdEndpoint), nameof(GetResponse),
            "Get deployment operation: subscription `{0}`, resource group `{1}`, deployment `{2}`, operationId `{3}`",
            subscriptionIdentifier, resourceGroupIdentifier, deploymentName, operationId);

        var deploymentOp = _controlPlane.GetDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName!);
        if (deploymentOp.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetRgScopeDirectory(
            subscriptionIdentifier.Value.ToString(), resourceGroupIdentifier.Value, deploymentName!);
        var record = OperationStore.GetAll(dir)
            .FirstOrDefault(r => string.Equals(r.OperationId, operationId, StringComparison.OrdinalIgnoreCase));

        if (record == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        response.CreateJsonContentResponse(record);
        response.Content.Headers.ContentType =
            System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
    }
}
