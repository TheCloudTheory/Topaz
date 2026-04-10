using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints.Registry;

internal sealed class CheckContainerRegistryNameAvailabilityEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.ContainerRegistry/checkNameAvailability"
    ];

    public string[] Permissions => ["Microsoft.ContainerRegistry/registries/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var path = context.Request.Path.Value!;
        var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var body = JsonSerializer.Deserialize<CheckNameAvailabilityRequest>(content, GlobalSettings.JsonOptions);

        if (body?.Name == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            response.Content = new StringContent(string.Empty);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return;
        }

        var isValidName = body.Name.Length is >= 5 and <= 50 && body.Name.All(char.IsLetterOrDigit);

        if (!isValidName)
        {
            response.StatusCode = HttpStatusCode.OK;
            response.Content = new StringContent(JsonSerializer.Serialize(new CheckNameAvailabilityResponse
            {
                NameAvailable = false,
                Reason = "Invalid",
                Message = $"The registry name '{body.Name}' is invalid. A registry name must be between 5-50 alphanumeric characters."
            }, GlobalSettings.JsonOptions));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return;
        }

        var isAvailable = _controlPlane.IsNameAvailable(subscriptionIdentifier, null, body.Name);
        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(JsonSerializer.Serialize(new CheckNameAvailabilityResponse
        {
            NameAvailable = isAvailable,
            Reason = isAvailable ? null : "AlreadyExists",
            Message = isAvailable ? null : $"The registry name '{body.Name}' is already in use."
        }, GlobalSettings.JsonOptions));
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    private sealed class CheckNameAvailabilityRequest
    {
        public string? Name { get; init; }
        public string? Type { get; init; }
    }
}
