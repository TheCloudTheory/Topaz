using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ContainerRegistry.Endpoints.Registry;

internal sealed class ListReplicationsEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}/replications"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/replications/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var result = new { value = Array.Empty<object>() };
        var json = JsonSerializer.Serialize(result, GlobalSettings.JsonOptions);
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(json);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
