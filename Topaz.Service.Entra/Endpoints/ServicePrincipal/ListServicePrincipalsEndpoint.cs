using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

public class ListServicePrincipalsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /v1.0/servicePrincipals"
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        response.Content = new StringContent(new ServicePrincipalsListResponse().ToString());
        response.StatusCode = HttpStatusCode.OK;
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}