using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.User;

public class MeEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /me",
        "GET /v1.0/me",
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        if (context.User.Identity is null)
        {
            logger.LogDebug(nameof(MeEndpoint), nameof(GetResponse), "Principal is not authenticated.");
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;
        }
        
        var objectId = context.User.Claims!.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(objectId))
        {
            logger.LogDebug(nameof(MeEndpoint), nameof(GetResponse), "Claim was found but value was null or empty.");
            response.StatusCode = HttpStatusCode.Unauthorized;
            return;       
        }
        
        var operation = _dataPlane.Get(UserIdentifier.From(objectId));
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            logger.LogDebug(nameof(MeEndpoint), nameof(GetResponse), "User not found.");
            response.StatusCode = HttpStatusCode.BadRequest;
            return;       
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(GetUserResponse.From(operation.Resource).ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}