using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class GetGroupOwnersEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string OwnersResponse =
        "{\"value\":[{\"@odata.type\":\"#microsoft.graph.user\",\"id\":\"00000000-0000-0000-0000-000000000000\"}]}";

    public string[] Endpoints =>
    [
        "GET /groups/{groupId}/owners",
        "GET /v1.0/groups/{groupId}/owners",
        "GET /beta/groups/{groupId}/owners",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetGroupOwnersEndpoint), nameof(GetResponse),
            "Returning owners for group.");

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(OwnersResponse);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
