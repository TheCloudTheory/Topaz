using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ValidateDeploymentAtSubscriptionScopeEndpoint(
    Pipeline eventPipeline,
    ITopazLogger logger,
    TemplateDeploymentOrchestrator orchestrator) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly SubscriptionDeploymentControlPlane _controlPlane =
        new(new SubscriptionDeploymentResourceProvider(logger), orchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.Resources/deployments/{deploymentName}/validate"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/validate/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var deploymentName = path.ExtractValueFromPath(6);

        logger.LogDebug(nameof(ValidateDeploymentAtSubscriptionScopeEndpoint), nameof(GetResponse),
            "Subscription `{0}`, deployment name: `{1}`", subscriptionIdentifier, deploymentName);

        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(subscriptionOperation.ToString());
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();

        content = content.Replace(", template:{", ", \"template\":{");

        logger.LogDebug(nameof(ValidateDeploymentAtSubscriptionScopeEndpoint), nameof(GetResponse),
            "Attempting to deserialize into {0}: {1}", nameof(CreateDeploymentRequest), content);

        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties?.Template == null)
        {
            response.CreateErrorResponse("InvalidTemplate", "The template is missing or invalid.",
                HttpStatusCode.BadRequest);
            return;
        }

        var result = _controlPlane.ValidateDeployment(subscriptionIdentifier, deploymentName!, request);
        if (result.Result == OperationResult.Success)
        {
            response.CreateJsonContentResponse(result.Resource!);
        }
        else
        {
            response.CreateErrorResponse(result.Code ?? "InvalidTemplate",
                result.Reason ?? "Validation failed.", HttpStatusCode.BadRequest);
        }
    }
}
