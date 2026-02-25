using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.CloudEnvironment.Models.Responses;

namespace Topaz.CloudEnvironment.Endpoints;

internal sealed class OidcEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /organizations/v2.0/.well-known/openid-configuration",
        "GET /{tenantId}/v2.0/.well-known/openid-configuration",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([8899], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var config = new OpenIdConfigurationResponse();
        response.Content = new StringContent(config.ToString());
    }
}