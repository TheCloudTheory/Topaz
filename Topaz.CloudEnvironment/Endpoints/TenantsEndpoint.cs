using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.CloudEnvironment.Endpoints;

internal sealed class TenantsEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /tenants"
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (!options.TenantId.HasValue)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var metadata = new ListTenantsResponse(options.TenantId.Value);
        response.Content = new StringContent(metadata.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}