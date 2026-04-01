using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup;
using Topaz.Service.ResourceManager.Deployment;
using Topaz.Service.ResourceManager.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class ExportTemplateEndpoint(
    Pipeline eventPipeline,
    ITopazLogger logger,
    TemplateDeploymentOrchestrator deploymentOrchestrator) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _resourceGroupControlPlane =
        ResourceGroupControlPlane.New(eventPipeline, logger);

    private readonly ResourceManagerControlPlane _controlPlane =
        new(new ResourceManagerResourceProvider(logger), deploymentOrchestrator, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/exportTemplate",
    ];

    public string[] Permissions => ["Microsoft.Resources/subscriptions/resourceGroups/exportTemplate/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));

        var rgOperation = _resourceGroupControlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier);
        if (rgOperation.Result == OperationResult.NotFound || rgOperation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceGroupNotFoundCode,
                resourceGroupIdentifier.Value);
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var requestBody = reader.ReadToEnd();
        var exportRequest = string.IsNullOrWhiteSpace(requestBody)
            ? new ExportTemplateRequest()
            : JsonSerializer.Deserialize<ExportTemplateRequest>(requestBody, GlobalSettings.JsonOptions) ?? new ExportTemplateRequest();

        var result = _controlPlane.ExportTemplate(subscriptionIdentifier, resourceGroupIdentifier, exportRequest);
        response.CreateJsonContentResponse(result);
    }
}
