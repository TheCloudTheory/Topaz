using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class CreateOrUpdateDeploymentEndpoint(
    Pipeline eventPipeline,
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        ResourceGroupControlPlane.New(eventPipeline, logger);

    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}"
    ];

    public string[] Permissions => ["Microsoft.Resources/deployments/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
        var deploymentName = path.ExtractValueFromPath(8);

        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(subscriptionOperation.ToString());
            return;
        }

        var resourceGroupOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (resourceGroupOperation.Result == OperationResult.NotFound || resourceGroupOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(resourceGroupOperation.ToString());
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();

        // Attempt to fix broken Azure CLI serialization
        content = content.Replace(", template:{", ", \"template\":{");

        logger.LogDebug(nameof(CreateOrUpdateDeploymentEndpoint), nameof(GetResponse),
            "Attempting to deserialize into {0}: {1}", nameof(CreateDeploymentRequest), content);

        var request = JsonSerializer.Deserialize<CreateDeploymentRequest>(content, GlobalSettings.JsonOptions);
        if (request?.Properties == null || string.IsNullOrWhiteSpace(request.Properties.Mode))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.CreateOrUpdateDeployment(subscriptionIdentifier,
            resourceGroupOperation.Resource.GetResourceGroup(),
            deploymentName!,
            JsonSerializer.Serialize(request.Properties.Template),
            request.Properties.Parameters?.Parameters,
            resourceGroupOperation.Resource.Location!,
            request.Properties.Mode);

        response.CreateJsonContentResponse(result.resource, HttpStatusCode.Created);
    }
}
