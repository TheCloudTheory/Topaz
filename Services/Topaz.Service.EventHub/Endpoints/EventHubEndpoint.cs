using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubEndpoint : IEndpointDefinition
{
    public string? ProviderNamespace => "Microsoft.EventHub";

    public string[] Endpoints => ["/{eventHubPath}/messages"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultEventHubPort], Protocol.Http);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
    }
}