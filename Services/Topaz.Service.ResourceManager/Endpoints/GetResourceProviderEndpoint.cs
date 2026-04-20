using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceManager.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceManager.Endpoints;

public sealed class GetResourceProviderEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _subscriptionControlPlane =
        SubscriptionControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/{providerName}"
    ];

    public string[] Permissions => ["Microsoft.Resources/providers/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
        var providerName = path.ExtractValueFromPath(4);

        var subscriptionOperation = _subscriptionControlPlane.Get(subscriptionIdentifier);
        if (subscriptionOperation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            response.Content = new StringContent(subscriptionOperation.ToString());
            return;
        }

        var data = new ResourceProviderDataResponse(providerName!)
        {
            Id = $"/subscriptions/{subscriptionIdentifier}/providers/{providerName}",
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(data.ToString());
    }
}
