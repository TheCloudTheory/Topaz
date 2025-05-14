using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;

namespace Topaz.Service.Storage.Endpoints;

public class BlobEndpoint : IEndpointDefinition
{
    public (int Port, Protocol Protocol) PortAndProtocol => (8891, Protocol.Https);

    public string[] Endpoints => ["blob.storage"];

    public BlobEndpoint()
    {
        
    }

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        var response = new HttpResponseMessage();

        switch (path)
        {
            case "/Blob":

            default:
                response.StatusCode = System.Net.HttpStatusCode.NotFound;
                break;
        }
            
        return response;
    }
}
