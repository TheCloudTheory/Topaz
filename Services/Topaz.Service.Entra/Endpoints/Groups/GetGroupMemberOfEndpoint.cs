using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class GetGroupMemberOfEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string EmptyMemberOfResponse = "{\"value\":[]}";

    public string[] Endpoints =>
    [
        "GET /groups/{groupId}/memberOf",
        "GET /v1.0/groups/{groupId}/memberOf",
        "GET /beta/groups/{groupId}/memberOf",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetGroupMemberOfEndpoint), nameof(GetResponse),
            "Returning memberOf for group.");

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(EmptyMemberOfResponse);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
