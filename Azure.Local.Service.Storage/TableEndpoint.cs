using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class TableEndpoint : IEndpointDefinition
{
    public int PortNumber => 10001;

    public Protocol Protocol => Protocol.Http;

    public string GetResponse(Stream input)
    {
        return "Response from Table Endpoint";
    }
}
