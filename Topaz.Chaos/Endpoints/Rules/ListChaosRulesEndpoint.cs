using Microsoft.AspNetCore.Http;
using Topaz.Chaos.Models;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Chaos.Endpoints.Rules;

internal sealed class ListChaosRulesEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    public string[] Endpoints => ["GET /topaz/chaos/rules"];
    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListChaosRulesEndpoint), nameof(GetResponse), "Listing chaos rules.");
        response.CreateJsonContentResponse(new ChaosRulesListResponse { Value = ChaosRulesProvider.ListOrdered() });
    }
}
