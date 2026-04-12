using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class GetGroupMembersEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string EmptyMembersResponse = "{\"value\":[]}";

    public string[] Endpoints =>
    [
        "GET /groups/{groupId}/members",
        "GET /v1.0/groups/{groupId}/members",
        "GET /beta/groups/{groupId}/members",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetGroupMembersEndpoint), nameof(GetResponse),
            "Returning members for group.");

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(EmptyMembersResponse);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
