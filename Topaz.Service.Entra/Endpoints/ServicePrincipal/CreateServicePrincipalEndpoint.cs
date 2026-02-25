using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints.ServicePrincipal;

public class CreateServicePrincipalEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServicePrincipalDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "POST /servicePrincipals",
    ];

    public string[] Permissions => [];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);
    
    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(CreateServicePrincipalEndpoint), nameof(GetResponse),
            "Creating a service principal.");
        
        using var reader = new StreamReader(context.Request.Body);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateServicePrincipalRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _dataPlane.Create(request);
        if (operation.Result != OperationResult.Created  ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing CreateUser operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.Created;
        response.Content = new StringContent(operation.Resource.ToString());
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}