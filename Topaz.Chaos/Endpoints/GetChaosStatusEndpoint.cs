using Microsoft.AspNetCore.Http;
using Topaz.Chaos.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints;

internal sealed class GetChaosStatusEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints =>
    [
        "GET /topaz/chaos/status"
    ];
    
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(GetChaosStatusEndpoint), nameof(GetResponse), "Returning chaos status.");
        
        response.CreateJsonContentResponse(new ChaosStateResponse
        {
            Enabled = ChaosStateProvider.IsEnabled
        });
    }
}