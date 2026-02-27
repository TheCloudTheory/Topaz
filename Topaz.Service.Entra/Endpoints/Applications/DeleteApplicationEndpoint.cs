using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Applications;

internal sealed class DeleteApplicationEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationsDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "DELETE /applications/{applicationId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var applicationIdentifier = ApplicationIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(DeleteApplicationEndpoint), nameof(GetResponse),
            "Deleting an application `{0}`.", applicationIdentifier);
        
        var operation = _dataPlane.Delete(applicationIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }
}