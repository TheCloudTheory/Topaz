using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetDeploymentOperationAtSubscriptionScopeByIdEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly SubscriptionDeploymentControlPlane _controlPlane =
        new(new SubscriptionDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Resources/deployments/{deploymentName}/operations/{operationId}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var deploymentName = path.ExtractValueFromPath(6);
        var operationId = path.ExtractValueFromPath(8);

        logger.LogDebug(nameof(GetDeploymentOperationAtSubscriptionScopeByIdEndpoint), nameof(GetResponse),
            "Get subscription-scope deployment operation: subscription `{0}`, deployment `{1}`, operationId `{2}`",
            subscriptionIdentifier, deploymentName, operationId);

        var deploymentOp = _controlPlane.Get(subscriptionIdentifier, deploymentName!);
        if (deploymentOp.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        var dir = OperationStore.GetSubScopeDirectory(
            subscriptionIdentifier.Value.ToString(), deploymentName!);
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
