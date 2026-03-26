using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.Applications;

internal sealed class ListApplicationsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationsDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /v1.0/applications",
        "GET /applications"
    ];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListApplicationsEndpoint), nameof(GetResponse), "Fetching applications.");
        
        var operation = _dataPlane.ListApplications();
        var result = ListApplicationsResponse.From(operation.Resource);
        
        if (context.Request.Query.ContainsKey("$count"))
        {
            logger.LogDebug(nameof(ListApplicationsEndpoint), nameof(GetResponse),
                "$count parameter was provided - calculating the total number of applications.");
            
            result.OdataCount = operation.Resource!.Length;
        }
        
        response.CreateJsonContentResponse(result);
    }
}