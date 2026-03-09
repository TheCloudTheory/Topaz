using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

internal sealed class UpdateSubscriptionTagsSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/tagNames/{tagName}/tagValues/{tagValue}",
    ];

    public string[] Permissions =>
    [
        "Microsoft.Resources/subscriptions/write"
    ];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        if (subscriptionId == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent($"Subscription ID can't be null.");
            return;
        }

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var tagName = context.Request.Path.Value.ExtractValueFromPath(4);
        var tagValue = context.Request.Path.Value.ExtractValueFromPath(6);
        if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(tagValue))
        {
            logger.LogDebug(nameof(UpdateSubscriptionTagsSubscriptionEndpoint), nameof(GetResponse), "Invalid tag name or value.");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.UpdateTags(subscriptionIdentifier, tagName, tagValue);
        if (operation.Result is OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent($"Subscription with ID {subscriptionId} doesn't exist.");

            return;
        }
        
        response.CreateJsonContentResponse(UpdateSubscriptionTagsSubscriptionResponse.From(subscriptionId, tagName, tagValue));
    }
}