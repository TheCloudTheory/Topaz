using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubAmqpEndpoint : IEndpointDefinition
{
    public string[] Endpoints => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultEventHubAmqpPort], Protocol.Amqp);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options)
    {
        throw new NotImplementedException();
    }
}