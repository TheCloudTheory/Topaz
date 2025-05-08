using Microsoft.AspNetCore.Http;

namespace Azure.Local.Service.Shared;

public interface IEndpointDefinition
{
    public Protocol Protocol { get; }
    public string[] Endpoints { get; }
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query);
}
