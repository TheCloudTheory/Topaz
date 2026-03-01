using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.Applications;

internal sealed class GetApplicationEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ApplicationsDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /applications/{applicationId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var applicationIdentifier =
            ApplicationIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        
        logger.LogDebug(nameof(GetApplicationEndpoint), nameof(GetResponse),
            "Fetching an application `{0}`.", applicationIdentifier);
        
        var operation = _dataPlane.Get(applicationIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}