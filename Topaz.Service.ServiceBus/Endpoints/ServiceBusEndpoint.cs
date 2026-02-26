using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Endpoints;

public sealed class ServiceBusEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["*"];
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultServiceBusAmqpPort], Protocol.Amqp);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }
}