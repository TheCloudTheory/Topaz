using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;

namespace Topaz.Service.Entra;

internal sealed class EntraGraphEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /v1.0/servicePrincipals"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol  => ([8899], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();
        
        HandleGetServicePrincipalsRequest(response);
        
        return response;
    }
    
    private void HandleGetServicePrincipalsRequest(HttpResponseMessage response)
    {
        response.Content = new StringContent(new ServicePrincipalsListResponse().ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}