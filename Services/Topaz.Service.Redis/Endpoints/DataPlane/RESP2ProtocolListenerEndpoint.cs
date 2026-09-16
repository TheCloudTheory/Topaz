using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Redis.Endpoints.DataPlane;

internal sealed class Resp2ProtocolListenerEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly Resp2ProtocolHandler _handler = new(RedisServiceControlPlane.New(eventPipeline, logger), logger);
    public string[] Endpoints => ["/"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.RedisPort], Protocol.Tcp);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }

    public async Task HandleTcpConnection(Socket socket)
    {
        var receiveBuffer = new byte[4096];
        while (socket.Connected)
        {
            var noOfBytes = await socket.ReceiveAsync(receiveBuffer);
            if (noOfBytes == 0)
            {
                logger.LogDebug(nameof(Resp2ProtocolListenerEndpoint), nameof(HandleTcpConnection), "Connection closed by client.");
                return;
            }

            var response = _handler.Handle(receiveBuffer, noOfBytes);
            await socket.SendAsync(response);
        }
    }
}