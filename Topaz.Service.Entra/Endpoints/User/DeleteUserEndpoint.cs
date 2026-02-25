using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.User;

public class DeleteUserEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "DELETE /users/{userId}",
        "DELETE /v1.0/users/{userId}"
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var userIdentifier = context.Request.Path.Value.StartsWith("/users") ? 
            UserIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : UserIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));
        
        logger.LogDebug(nameof(DeleteUserEndpoint), nameof(GetResponse), "Deleting a user `{0}`.",  userIdentifier);
        
        var operation = _dataPlane.Delete(userIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }
}