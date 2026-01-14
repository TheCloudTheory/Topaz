using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Service.Shared;

namespace Topaz.CloudEnvironment.Endpoints;

internal sealed class TenantsEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /tenants"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([8899], Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        if (!options.TenantId.HasValue)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return response;
        }
        
        var metadata = new ListTenantsResponse(options.TenantId.Value);
        
        response.Content = new StringContent(metadata.ToString());
       
        return response;
    }
}