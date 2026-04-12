using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

public class DeleteServicePrincipalEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServicePrincipalDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "DELETE /servicePrincipals/{servicePrincipalId}",
        "DELETE /v1.0/servicePrincipals/{servicePrincipalId}",
        "DELETE /beta/servicePrincipals/{servicePrincipalId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var servicePrincipalIdentifier = context.Request.Path.Value.StartsWith("/servicePrincipals")
            ? ServicePrincipalIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : ServicePrincipalIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));
        
        logger.LogDebug(nameof(DeleteServicePrincipalEndpoint), nameof(GetResponse),
            "Deleting a service principal `{0}`.", servicePrincipalIdentifier);
        
        var operation = _dataPlane.Delete(servicePrincipalIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }
}