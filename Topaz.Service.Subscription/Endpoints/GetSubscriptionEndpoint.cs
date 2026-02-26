using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

public class GetSubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}",
    ];

    public string[] Permissions => [
        "Microsoft.Resources/subscriptions/read"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var operation = _controlPlane.Get(SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2)));
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        response.Content = JsonContent.Create(operation.Resource, new MediaTypeHeaderValue("application/json"), GlobalSettings.JsonOptions);
        response.StatusCode = HttpStatusCode.OK;
    }
}