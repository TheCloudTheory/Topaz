using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CancelDeploymentAtSubscriptionScopeEndpoint(
    Pipeline eventPipeline,
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly SubscriptionDeploymentControlPlane _controlPlane =
        new(new SubscriptionDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.Resources/deployments/{deploymentName}/cancel"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/cancel/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var deploymentName = path.ExtractValueFromPath(6);

        logger.LogDebug(nameof(CancelDeploymentAtSubscriptionScopeEndpoint), nameof(GetResponse),
            "Cancel subscription-scope deployment: subscription `{0}`, deployment `{1}`",
            subscriptionIdentifier, deploymentName);

        var result = _controlPlane.CancelDeployment(subscriptionIdentifier, deploymentName!);

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
