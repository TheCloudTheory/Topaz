using Microsoft.AspNetCore.Http;
using Topaz.Chaos.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints;

internal sealed class DisableChaosEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "POST /topaz/chaos/disable"
    ];
    
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        ChaosStateProvider.IsEnabled = false;
        logger.LogDebug(nameof(DisableChaosEndpoint), nameof(GetResponse), "Chaos disabled.");
        
        response.CreateJsonContentResponse(new ChaosStateResponse
        {
            Enabled = false
        });
    }
}