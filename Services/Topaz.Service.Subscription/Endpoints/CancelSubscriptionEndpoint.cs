using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

internal sealed class CancelSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.Subscription/cancel",
    ];

    public string[] Permissions =>
    [
        "Microsoft.Subscription/cancel/action"
    ];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(2);
        if (subscriptionId == null)
        {
            response.CreateErrorResponse("InvalidRequest", "Subscription ID can't be null.", HttpStatusCode.BadRequest);
            return;
        }

        var subscriptionIdentifier = SubscriptionIdentifier.From(subscriptionId);
        var operation = _controlPlane.Cancel(subscriptionIdentifier);

        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.NotFound);
            return;
        }

        var cancelResponse = new CancelSubscriptionResponse
        {
            SubscriptionId = subscriptionId,
            CancellationId = Guid.NewGuid().ToString()
        };

        response.CreateJsonContentResponse(cancelResponse);
    }
}
