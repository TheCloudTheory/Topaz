using Microsoft.AspNetCore.Http;

namespace Topaz.Service.Shared;

public interface IEndpointDefinition
{
    public string[] Endpoints { get; }
    public string[] Permissions { get; }
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol { get; }

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options);
}
