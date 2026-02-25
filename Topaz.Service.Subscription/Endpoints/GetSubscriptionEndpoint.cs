using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Subscription.Endpoints;

public class GetSubscriptionEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(logger);
    
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}",
    ];

    public string[] Permissions => [];
    
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