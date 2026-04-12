using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Entra.Domain;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Entra.Endpoints.User;

internal sealed class UpdateUserEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly UserDataPlane _dataPlane = new(new EntraResourceProvider(logger), logger);

    public string[] Endpoints =>
    [
        "PATCH /v1.0/users/{userId}",
        "PATCH /beta/users/{userId}",
        "PATCH /users/{userId}",
    ];

    public string[] Permissions => ["*"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var userIdentifier = context.Request.Path.Value.StartsWith("/users")
            ? UserIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2))
            : UserIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(3));

        logger.LogDebug(nameof(UpdateUserEndpoint), nameof(GetResponse), "Updating a user `{0}`.", userIdentifier);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<UpdateUserRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _dataPlane.Update(userIdentifier, request);
        if (operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing UpdateUser operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
        response.Content = new ByteArrayContent([]);
        response.Content.Headers.ContentType = null;
    }
}
