using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubAmqpEndpoint : IEndpointDefinition
{
    public string[] Endpoints { get; }
    public (int Port, Protocol Protocol) PortAndProtocol => (8898, Protocol.Amqp);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query)
    {
        throw new NotImplementedException();
    }
}