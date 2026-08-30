using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics;

internal sealed class ListEventGridTopicEventTypesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly EventGridTopicControlPlane _controlPlane =
        EventGridTopicControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.EventGrid";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{providerNamespace}/{resourceTypeName}/{resourceName}/providers/Microsoft.EventGrid/eventTypes"
    ];

    public string[] Permissions => ["Microsoft.EventGrid/topictypes/eventtypes/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(8);

        var result = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, topicName!);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(EventTypesListResultResponse.From(result.Resource));
    }
}