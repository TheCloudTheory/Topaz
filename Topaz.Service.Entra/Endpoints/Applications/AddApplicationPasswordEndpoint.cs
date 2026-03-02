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

internal sealed class AddApplicationPasswordEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationsDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "POST /v1.0/applications/{applicationId}/addPassword",
        "POST /applications/{applicationId}/addPassword",
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var applicationIdentifier = context.Request.Path.Value.StartsWith("/v1.0")
            ? ApplicationIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3))
            : ApplicationIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(AddApplicationPasswordEndpoint), nameof(GetResponse),
            "Add a password to an application `{0}`.", applicationIdentifier);
        
        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<AddApplicationPasswordRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _dataPlane.AddPassword(applicationIdentifier, request);
        if (operation.Result != OperationResult.Created || operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing AddApplicationPassword operation.");
            return;
        }
        
        response.CreateJsonContentResponse(operation.Resource);
    }
}