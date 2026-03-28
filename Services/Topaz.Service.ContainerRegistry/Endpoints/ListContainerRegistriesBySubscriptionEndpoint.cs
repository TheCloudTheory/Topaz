using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

internal sealed class ListContainerRegistriesBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.ContainerRegistry/registries"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));

        var operation = _controlPlane.ListBySubscription(subscriptionIdentifier);
        var result = new ListContainerRegistriesResponse
        {
            Value = operation.Resource!.Select(ListContainerRegistriesResponse.ContainerRegistry.From).ToArray()
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
