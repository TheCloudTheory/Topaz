using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Endpoints;

public sealed class ServiceBusEndpoint : IEndpointDefinition
{
    public string[] Endpoints => [];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultServiceBusAmqpPort, Protocol.Amqp);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        throw new NotImplementedException();
    }
}