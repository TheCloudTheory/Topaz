using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics.SystemTopicSubscriptions;

internal sealed class DeleteEventGridSystemTopicSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly EventGridSystemTopicControlPlane _controlPlane =
        EventGridSystemTopicControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.EventGrid";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/systemTopics/{topicName}/eventSubscriptions/{eventSubscriptionName}"
    ];

    public string[] Permissions => ["Microsoft.EventGrid/topics/eventSubscriptions/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(8);
        var eventSubscriptionName = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(topicName) || string.IsNullOrWhiteSpace(eventSubscriptionName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.DeleteEventSubscription(subscriptionIdentifier, resourceGroupIdentifier, topicName, eventSubscriptionName);
        if (result.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        if (result.Result != OperationResult.Deleted)
        {
            response.CreateErrorResponse(result);
            return;
        }
        
        response.CreateNoContentResponse();
    }
}