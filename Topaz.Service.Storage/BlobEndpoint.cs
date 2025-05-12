using Topaz.Service.Shared;
using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Storage;

public class BlobEndpoint : IEndpointDefinition
{
    public Protocol Protocol => Protocol.Http;

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
