using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Requests;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

internal sealed class UpdateSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "PATCH /subscriptions/{subscriptionId}",
    ];

    public string[] Permissions => [];

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
        var subscription = _controlPlane.Get(subscriptionIdentifier);
        if (subscription.Result is OperationResult.NotFound)
        {
            response.CreateErrorResponse(subscription.Code!, subscription.Reason!, HttpStatusCode.NotFound);
            return;
        }

        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateSubscriptionRequest>(content, GlobalSettings.JsonOptions);

        var updateOperation = _controlPlane.Update(subscriptionIdentifier, request!);
        if (updateOperation.Result != OperationResult.Updated || updateOperation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent($"Failed to update subscription with ID {subscriptionId}.");
            return;
        }
        
        response.CreateJsonContentResponse(updateOperation.Resource);
    }
}