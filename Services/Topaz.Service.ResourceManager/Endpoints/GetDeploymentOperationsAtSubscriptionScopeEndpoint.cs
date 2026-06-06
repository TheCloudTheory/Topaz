using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

/// <summary>
/// Returns the list of operations for a subscription-scope deployment.
/// Azure PowerShell's Stop-AzDeployment calls this endpoint to check whether the
/// deployment is still running before issuing the cancel request.
/// </summary>
public sealed class GetDeploymentOperationsAtSubscriptionScopeEndpoint(
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly SubscriptionDeploymentControlPlane _controlPlane =
        new(new SubscriptionDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Resources/deployments/{deploymentName}/operations"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var deploymentName = path.ExtractValueFromPath(6);

        logger.LogDebug(nameof(GetDeploymentOperationsAtSubscriptionScopeEndpoint), nameof(GetResponse),
            "Get subscription-scope deployment operations: subscription `{0}`, deployment `{1}`",
            subscriptionIdentifier, deploymentName);

        var deploymentOp = _controlPlane.Get(subscriptionIdentifier, deploymentName!);
        if (deploymentOp.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new ByteArrayContent([]);
            return;
        }

        response.CreateJsonContentResponse(new DeploymentOperationsListResult([]));
    }

    private sealed class DeploymentOperationsListResult(object[] value)
    {
        public object[] Value { get; } = value;

        public override string ToString() =>
            System.Text.Json.JsonSerializer.Serialize(this, GlobalSettings.JsonOptions);
    }
}
