using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

internal sealed class UpdateServicePrincipalEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServicePrincipalDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "PATCH /servicePrincipals/{servicePrincipalId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var servicePrincipalIdentifier = ServicePrincipalIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(UpdateServicePrincipalEndpoint), nameof(GetResponse),
            "Updating a service principal `{0}`.", servicePrincipalIdentifier);
        
        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<UpdateServicePrincipalRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _dataPlane.Update(servicePrincipalIdentifier, request);
        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing UpdateServicePrincipal operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}