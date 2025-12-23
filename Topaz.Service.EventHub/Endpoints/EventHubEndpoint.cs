using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ITopazLogger _topazLogger = logger;
    public string[] Endpoints => ["/{eventHubPath}/messages"];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultEventHubPort, Protocol.Http);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options, Guid correlationId)
    {
        throw new NotImplementedException();
    }
}