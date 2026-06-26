using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;

namespace Topaz.ForwardProxy;

internal sealed class ForwardProxyEndpoint : IEndpointDefinition
{
    public string[] Endpoints => [];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([ForwardProxySettings.DefaultPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }
}