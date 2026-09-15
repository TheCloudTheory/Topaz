using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;

namespace Topaz.Service.Redis.Endpoints.DataPlane;

internal sealed class Resp2ProtocolListenerEndpoint : IEndpointDefinition
{
    public string[] Endpoints => ["/"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([6379], Protocol.Tcp);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }
    
    public async Task HandleTcpConnection(Socket socket)
    {
        var receiveBuffer = new byte[4096];
        var noOfBytes = await socket.ReceiveAsync(receiveBuffer);
        var data = Encoding.UTF8.GetString(receiveBuffer);
    }
}