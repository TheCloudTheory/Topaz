using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.EventGrid.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Shared.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.EventGrid.Endpoints.ControlPlane.Topics.SystemTopics;

internal sealed class ListEventGridSystemTopicBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly EventGridSystemTopicControlPlane _controlPlane =
        EventGridSystemTopicControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.EventGrid";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.EventGrid/systemTopics"
    ];

    public string[] Permissions => ["Microsoft.EventGrid/systemTopics/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        _ = context.Request.QueryString.TryGetValueForKey("$top", out var topFilter);

        var result = _controlPlane.ListBySubscription(subscriptionIdentifier, topFilter);
        if (result.Result == OperationResult.NotFound || result.Resource == null)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!, HttpStatusCode.NotFound);
            return;
        }

        response.CreateJsonContentResponse(
            ResourcesListResultResponseBase<EventGridSystemTopicResource>.From(result.Resource));
    }
}