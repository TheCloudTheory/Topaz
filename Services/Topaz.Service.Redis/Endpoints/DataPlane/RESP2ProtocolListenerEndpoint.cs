using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis.Endpoints.DataPlane;

internal sealed class Resp2ProtocolListenerEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["/"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.RedisPort, GlobalSettings.RedisSslPort], Protocol.Tcp);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }

    public async Task HandleTcpConnection(Socket socket)
    {
        var receiveBuffer = new byte[4096];
        var handler = new Resp2ProtocolHandler(RedisServiceControlPlane.New(eventPipeline, logger), logger);
        
        while (socket.Connected)
        {
            var noOfBytes = await socket.ReceiveAsync(receiveBuffer);
            if (noOfBytes == 0)
            {
                logger.LogDebug(nameof(Resp2ProtocolListenerEndpoint), nameof(HandleTcpConnection), "Connection closed by client.");
                return;
            }

            
            var response = handler.Handle(receiveBuffer, noOfBytes);
            await socket.SendAsync(response);
        }
    }
}