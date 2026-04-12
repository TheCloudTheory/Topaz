using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

internal sealed class RemoveServicePrincipalOwnerEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "DELETE /servicePrincipals/{spId}/owners/{userId}/$ref",
        "DELETE /v1.0/servicePrincipals/{spId}/owners/{userId}/$ref",
        "DELETE /beta/servicePrincipals/{spId}/owners/{userId}/$ref",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(RemoveServicePrincipalOwnerEndpoint), nameof(GetResponse),
            "Removing owner from service principal (no-op).");

        response.StatusCode = HttpStatusCode.NoContent;
        response.Content = new ByteArrayContent([]);
    }
}
