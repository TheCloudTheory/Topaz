using Azure.Local.Service.Shared;

namespace Azure.Local.Service.Storage;

public class TableEndpoint : IEndpointDefinition
{
    public int PortNumber => 10001;

    public Protocol Protocol => Protocol.Http;
}
