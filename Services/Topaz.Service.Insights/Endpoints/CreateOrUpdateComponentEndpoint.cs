using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Insights.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Insights.Endpoints;

internal sealed class CreateOrUpdateComponentEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApplicationInsightsServiceControlPlane _controlPlane =
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "microsoft.insights";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/microsoft.insights/components/{componentName}"
    ];

    public string[] Permissions => ["microsoft.insights/components/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(name))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<ApplicationInsightsComponentResource>(reader.ReadToEnd(), GlobalSettings.JsonOptions);
        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.CreateOrUpdate(sub, rg, name, request);
        if (result.Result is not (OperationResult.Created or OperationResult.Updated) || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.StatusCode = result.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.CreateJsonContentResponse(result.Resource);
    }
}
