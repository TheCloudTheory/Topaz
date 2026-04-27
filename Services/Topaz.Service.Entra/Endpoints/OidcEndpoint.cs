using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

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
        // When called via /{tenantId}/v2.0/.well-known/openid-configuration, MSAL validates that
        // the issuer in the response contains the requested tenant ID. Return a tenant-specific
        // response in that case so authority validation succeeds for client-credential flows.
        var tenantId = context.Request.Path.Value?.ExtractValueFromPath(1);
        var config = tenantId is not null and not "organizations"
            ? new OpenIdConfigurationResponse(tenantId)
            : new OpenIdConfigurationResponse();
        response.Content = new StringContent(config.ToString());
    }
}