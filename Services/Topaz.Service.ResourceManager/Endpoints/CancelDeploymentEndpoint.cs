using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CancelDeploymentEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}/cancel"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/cancel/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var deploymentName = path.ExtractValueFromPath(8);

        logger.LogDebug(nameof(CancelDeploymentEndpoint), nameof(GetResponse),
            "Cancel deployment: subscription `{0}`, resource group `{1}`, deployment `{2}`",
            subscriptionIdentifier, resourceGroupIdentifier, deploymentName);

        var result = _controlPlane.CancelDeployment(subscriptionIdentifier, resourceGroupIdentifier, deploymentName!);

        response.StatusCode = result switch
        {
            OperationResult.Success => HttpStatusCode.NoContent,
            OperationResult.NotFound => HttpStatusCode.NotFound,
            OperationResult.Conflict => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };

        response.Content = new ByteArrayContent([]);
    }
}
