using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;

namespace Topaz.Service.Entra;

internal sealed class EntraGraphEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /v1.0/servicePrincipals",
        "GET /me"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol  => ([8899], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        switch (method)
        {
            case "GET":
                if (path == "/me")
                {
                    HandleMeRequest(response);
                    break;
                }
                
                HandleGetServicePrincipalsRequest(response);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
        
        return response;
    }

    private static void HandleMeRequest(HttpResponseMessage response)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(new GetUserResponse().ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    private void HandleGetServicePrincipalsRequest(HttpResponseMessage response)
    {
        response.Content = new StringContent(new ServicePrincipalsListResponse().ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}