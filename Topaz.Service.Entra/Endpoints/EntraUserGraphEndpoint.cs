using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Models.Responses;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra.Endpoints;

internal sealed class EntraUserGraphEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "GET /me",
        "POST /v1.0/users",
        "POST /users"
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

                break;
            case "POST":
                HandleCreateUserRequest(response, input);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
        
        return response;
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
        
        var operation = _dataPlane.CreateUser(request);
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
        
        // It's important to set the content type header for response because Graph SDK
        // checks for its value and if can't find it, it fallbacks to `null` result.
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }
}