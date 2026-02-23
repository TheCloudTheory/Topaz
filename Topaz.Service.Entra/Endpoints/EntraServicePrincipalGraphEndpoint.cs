using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints;

internal sealed class EntraServicePrincipalGraphEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServicePrincipalDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /v1.0/servicePrincipals",
        "GET /servicePrincipals/{servicePrincipalId}",
        "POST /servicePrincipals",
        "DELETE /servicePrincipals/{servicePrincipalId}"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol  => ([8899], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        switch (method)
        {
            case "GET":
                if (path.EndsWith("/servicePrincipals"))
                {
                    HandleListServicePrincipalsRequest(response);
                    break;
                }
                
                HandleGetServicePrincipalsRequest(response, ServicePrincipalIdentifier.From(path.ExtractValueFromPath(2)));
                break;
            case "POST":
                HandleCreateServicePrincipalRequest(response, input);
                break;
            case "DELETE":
                HandleDeleteServicePrincipalRequest(response, ServicePrincipalIdentifier.From(path.ExtractValueFromPath(2)));
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
        
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return response;
    }

    private void HandleGetServicePrincipalsRequest(HttpResponseMessage response, ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(EntraServicePrincipalGraphEndpoint), nameof(HandleGetServicePrincipalsRequest),
            "Fetching a service principal `{0}`.", servicePrincipalIdentifier);
        
        var operation = _dataPlane.Get(servicePrincipalIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleDeleteServicePrincipalRequest(HttpResponseMessage response, ServicePrincipalIdentifier servicePrincipalIdentifier)
    {
        logger.LogDebug(nameof(EntraServicePrincipalGraphEndpoint), nameof(HandleDeleteServicePrincipalRequest),
            "Deleting a service principal `{0}`.", servicePrincipalIdentifier);
        
        var operation = _dataPlane.Delete(servicePrincipalIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }

    private void HandleCreateServicePrincipalRequest(HttpResponseMessage response, Stream input)
    {
        logger.LogDebug(nameof(EntraServicePrincipalGraphEndpoint), nameof(HandleCreateServicePrincipalRequest),
            "Creating a service principal.");
        
        using var reader = new StreamReader(input);

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
    }

    private void HandleListServicePrincipalsRequest(HttpResponseMessage response)
    {
        response.Content = new StringContent(new ServicePrincipalsListResponse().ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}