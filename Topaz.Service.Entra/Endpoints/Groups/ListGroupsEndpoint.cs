using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Groups;

internal sealed class ListGroupsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly GroupDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints => ["GET /groups"];
    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListGroupsEndpoint), nameof(GetResponse), "Fetching groups.");

        var operation = _dataPlane.ListServicePrincipals();

        response.CreateJsonContentResponse(ListGroupsResponse.From(operation.Resource!));
    }
}