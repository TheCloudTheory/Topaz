using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

internal sealed class GetServicePrincipalOwnersEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private const string OwnersResponse =
        "{\"value\":[{\"@odata.type\":\"#microsoft.graph.user\",\"id\":\"00000000-0000-0000-0000-000000000000\"}]}";

    public string[] Endpoints =>
    [
        "GET /servicePrincipals/{spId}/owners",
        "GET /v1.0/servicePrincipals/{spId}/owners",
        "GET /beta/servicePrincipals/{spId}/owners",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetServicePrincipalOwnersEndpoint), nameof(GetResponse),
            "Returning owners for service principal.");

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(OwnersResponse);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}
