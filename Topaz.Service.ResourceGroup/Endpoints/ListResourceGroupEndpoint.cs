using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ResourceGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ResourceGroup.Endpoints;

public class ListResourceGroupEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceGroupControlPlane _controlPlane = ResourceGroupControlPlane.New(eventPipeline, logger);
    
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var operation = _controlPlane.List(subscriptionIdentifier);
        if (operation.result == OperationResult.Failed)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(new ListResourceGroupsResponse(operation.resources).ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}