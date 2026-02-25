using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;

namespace Topaz.Service.Subscription.Endpoints;

public class ListSubscriptionsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly SubscriptionControlPlane _controlPlane = SubscriptionControlPlane.New(logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var operation = _controlPlane.List();
        if (operation.result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var subscriptions = new ListSubscriptionsResponse(operation.resource);
        
        response.Content = new StringContent(subscriptions.ToString(), Encoding.UTF8, "application/json");
        response.StatusCode = HttpStatusCode.OK;
    }
}