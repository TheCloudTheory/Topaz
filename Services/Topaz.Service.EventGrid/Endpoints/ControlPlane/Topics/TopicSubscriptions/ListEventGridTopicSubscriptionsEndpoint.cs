using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Shared.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics.TopicSubscriptions;

internal sealed class ListEventGridTopicSubscriptionsEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly EventGridTopicControlPlane _controlPlane =
        EventGridTopicControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.EventGrid";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/topics/eventSubscriptions"
    ];

    public string[] Permissions => ["Microsoft.EventGrid/topics/eventSubscriptions/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(8);
        _ = context.Request.QueryString.TryGetValueForKey("$top", out var topFilter);

        var result = _controlPlane.ListEventSubscriptions(subscriptionIdentifier, resourceGroupIdentifier, topicName!, topFilter);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(
            ResourcesListResultResponseBase<EventSubscriptionSubresource>.From(result.Resource));
    }
}