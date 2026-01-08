using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Shared;

public interface IEndpointDefinition
{
    public string[] Endpoints { get; }
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol { get; }
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query, GlobalOptions options, Guid correlationId);
}
