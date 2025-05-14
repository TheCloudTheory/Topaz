using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub;

public sealed class EventHubEndpoint(ILogger logger) : IEndpointDefinition
{
    private readonly ILogger logger = logger;
    public string[] Endpoints => ["/{eventHubPath}/messages"];
    public (int Port, Protocol Protocol) PortAndProtocol => (8897, Protocol.Http);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        throw new NotImplementedException();
    }
}