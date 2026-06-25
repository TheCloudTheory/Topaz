using Microsoft.AspNetCore.Http;
using Topaz.Chaos.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints;

internal sealed class EnableChaosEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "POST /topaz/chaos/enable"
    ];
    
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        ChaosStateProvider.IsEnabled = true;
        logger.LogDebug(nameof(EnableChaosEndpoint), nameof(GetResponse), "Chaos enabled.");
        
        response.CreateJsonContentResponse(new ChaosStateResponse
        {
            Enabled = true
        });
    }
}