using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.User;

internal sealed class ListUsersEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /v1.0/users",
        "GET /users",
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListUsersEndpoint), nameof(GetResponse), "Fetching users.");
        
        var operation = _dataPlane.ListUsers();
        var result = ListUsersResponse.From(operation.Resource);

        if (context.Request.Query.ContainsKey("$count"))
        {
            logger.LogDebug(nameof(ListUsersEndpoint), nameof(GetResponse),
                "$count parameter was provided - calculating the total number of users.");
            
            result.OdataCount = operation.Resource!.Length;
        }
        
        response.CreateJsonContentResponse(result);
    }
}