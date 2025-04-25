using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class BlobEndpoint: IEndpointDefinition
{
    public Protocol Protocol => Protocol.Http;

    public string DnsName => "blob.storage";

    public string GetResponse(Stream input)
    {
        return "Response from Blob Endpoint";
    }
}
