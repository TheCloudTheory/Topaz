using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Shared;

namespace Topaz.Service.Entra.Endpoints;

internal sealed class EntraUserGraphEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /me",
        "POST /v1.0/users"
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

                break;
            case "POST":
                HandleCreateUserRequest(response, input);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
        
        return response;
    }

    private void HandleCreateUserRequest(HttpResponseMessage response, Stream input)
    {
        
    }

    private static void HandleMeRequest(HttpResponseMessage response)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(new GetUserResponse().ToString());
        
        // It's important to set the content type header for response because Graph SDK
        // checks for its value and if can't find it, it fallbacks to `null` result.
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}