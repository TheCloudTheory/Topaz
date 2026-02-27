using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Applications;

internal sealed class UpdateApplicationEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationsDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "PATCH /applications/{applicationId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var applicationIdentifier = ApplicationIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(UpdateApplicationEndpoint), nameof(GetResponse),
            "Updating an application `{0}`.", applicationIdentifier);
        
        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<UpdateApplicationRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _dataPlane.Update(applicationIdentifier, request);
        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing UpdateApplication operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}