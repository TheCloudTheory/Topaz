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
    public (int Port, Protocol Protocol) PortAndProtocol => (8899, Protocol.Https);
    
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options)
    {
        var response = new HttpResponseMessage();
        var metadata = new GetMetadataEndpointResponse();
        
        response.Content = new StringContent(metadata.ToString());
        
        return response;
    }
}