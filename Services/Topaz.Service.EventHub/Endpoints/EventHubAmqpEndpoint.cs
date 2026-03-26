using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubAmqpEndpoint : IEndpointDefinition
{
    public string[] Endpoints => [];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultEventHubAmqpPort], Protocol.Amqp);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }
}