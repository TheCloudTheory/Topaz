using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class BlobEndpoint : IEndpointDefinition
{
    public Protocol Protocol => Protocol.Http;

    public string DnsName => "blob.storage";

    public BlobEndpoint()
    {
        
    }

    public HttpResponseMessage GetResponse(string path, Stream input)
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
