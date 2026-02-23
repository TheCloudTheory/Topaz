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

internal sealed class EntraUserGraphEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /me",
        "GET /users/{userId}",
        "POST /v1.0/users",
        "POST /users",
        "POST /v1.0/users",
        "DELETE /users/{userId}",
        "DELETE /v1.0/users/{userId}"
    ];
    
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol  => ([8899], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        switch (method)
        {
            case "GET":
                if (path == "/me")
                {
                    HandleMeRequest(response);
                    break;
                }

                var userIdentifier = UserIdentifier.From(path.ExtractValueFromPath(2));
                HandleGetUserRequest(response, userIdentifier);
                
                break;
            case "POST":
                HandleCreateUserRequest(response, input);
                break;
            case "DELETE":
                HandleDeleteUserRequest(response, UserIdentifier.From(path.ExtractValueFromPath(2)));
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
        
        // It's important to set the content type header for response because Graph SDK
        // checks for its value and if can't find it, it fallbacks to `null` result.
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        
        return response;
    }

    private void HandleDeleteUserRequest(HttpResponseMessage response, UserIdentifier userIdentifier)
    {
        logger.LogDebug(nameof(EntraUserGraphEndpoint), nameof(HandleDeleteUserRequest), "Deleting a user `{0}`.",  userIdentifier);
        
        var operation = _dataPlane.Delete(userIdentifier);
        if (operation.Result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.NoContent;
    }

    private void HandleGetUserRequest(HttpResponseMessage response, UserIdentifier userIdentifier)
    {
        logger.LogDebug(nameof(EntraUserGraphEndpoint), nameof(HandleGetUserRequest), "Fetching a user `{0}`.", userIdentifier);
        
        var operation = _dataPlane.Get(userIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCreateUserRequest(HttpResponseMessage response, Stream input)
    {
        logger.LogDebug(nameof(EntraUserGraphEndpoint), nameof(HandleCreateUserRequest), "Creating a user.");
        
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateUserRequest>(content, GlobalSettings.JsonOptions);

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

    private static void HandleMeRequest(HttpResponseMessage response)
    {
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(new GetUserResponse().ToString());
    }
}