using Microsoft.AspNetCore.Http;
using Topaz.CloudEnvironment.Models.Responses;
using Topaz.Service.Shared;

namespace Topaz.CloudEnvironment.Endpoints;

public sealed class MetadataEndpoint : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /metadata/endpoints"
    ];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([8899], Protocol.Https);
    
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options, Guid correlationId)
    {
        var response = new HttpResponseMessage();
        var metadata = new GetMetadataEndpointResponse();
        
        response.Content = new StringContent(metadata.ToString());
        
        return response;
    }
}