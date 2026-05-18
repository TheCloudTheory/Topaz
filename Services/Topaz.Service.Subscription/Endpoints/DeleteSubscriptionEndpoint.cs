using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

internal sealed class DeleteSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}",
    ];

    public string[] Permissions => [];

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
        var existing = _controlPlane.Get(subscriptionIdentifier);
        if (existing.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NoContent;
            response.Content = new ByteArrayContent([]);
            response.Content.Headers.ContentType = null;
            return;
        }

        _controlPlane.Delete(subscriptionIdentifier);
        response.StatusCode = HttpStatusCode.NoContent;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
    }
}
