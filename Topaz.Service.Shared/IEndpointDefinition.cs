using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Shared;

public interface IEndpointDefinition
{
    public string[] Endpoints { get; }
    public (int Port, Protocol Protocol) PortAndProtocol { get; }
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query);
}
