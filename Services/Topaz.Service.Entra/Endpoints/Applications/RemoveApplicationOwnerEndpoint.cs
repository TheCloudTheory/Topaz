using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Applications;

internal sealed class RemoveApplicationOwnerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "DELETE /applications/{applicationId}/owners/{userId}/$ref",
        "DELETE /v1.0/applications/{applicationId}/owners/{userId}/$ref",
        "DELETE /beta/applications/{applicationId}/owners/{userId}/$ref",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(RemoveApplicationOwnerEndpoint), nameof(GetResponse),
            "Removing owner from application (no-op).");

        response.StatusCode = HttpStatusCode.NoContent;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
