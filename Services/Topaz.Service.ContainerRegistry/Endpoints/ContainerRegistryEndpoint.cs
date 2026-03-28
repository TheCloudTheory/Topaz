using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ContainerRegistry.Models.Requests;
using Topaz.Service.ContainerRegistry.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ContainerRegistry.Endpoints;

internal sealed class ContainerRegistryEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ContainerRegistryControlPlane _controlPlane = ContainerRegistryControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries",
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.ContainerRegistry/registries",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{registryName}",
        "POST /subscriptions/{subscriptionId}/providers/Microsoft.ContainerRegistry/checkNameAvailability"
    ];

    public string[] Permissions => ["*/read", "*/write", "*/delete", "*/action"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            var path = context.Request.Path.Value!;
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));

            switch (context.Request.Method)
            {
                case "PUT":
                    HandleCreateOrUpdateRequest(context, response, subscriptionIdentifier, path);
                    break;
                case "GET":
                    HandleGetRequest(context, response, subscriptionIdentifier, path);
                    break;
                case "DELETE":
                    HandleDeleteRequest(response, subscriptionIdentifier, path);
                    break;
                case "POST":
                    HandleCheckNameAvailabilityRequest(context, response, subscriptionIdentifier);
                    break;
                default:
                    response.StatusCode = HttpStatusCode.MethodNotAllowed;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }

        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    }

    private void HandleCreateOrUpdateRequest(HttpContext context, HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, string path)
    {
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(registryName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateContainerRegistryRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.CreateOrUpdate(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName,
            request);

        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, HttpStatusCode.BadRequest);
            return;
        }

        response.StatusCode = operation.Result == OperationResult.Created
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource!.ToString());
    }

    private void HandleGetRequest(HttpContext context, HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, string path)
    {
        // Determine if this is a list-by-subscription, list-by-resource-group, or get-single request
        // List by subscription: /subscriptions/{id}/providers/Microsoft.ContainerRegistry/registries
        // List by resource group: /subscriptions/{id}/resourceGroups/{rg}/providers/.../registries
        // Get single: /subscriptions/{id}/resourceGroups/{rg}/providers/.../registries/{name}

        var segment3 = path.ExtractValueFromPath(3); // "providers" or "resourceGroups"

        if (string.Equals(segment3, "providers", StringComparison.OrdinalIgnoreCase))
        {
            HandleListBySubscriptionRequest(response, subscriptionIdentifier);
            return;
        }

        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(registryName))
        {
            HandleListByResourceGroupRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupName));
        }
        else
        {
            HandleGetSingleRequest(response, subscriptionIdentifier, ResourceGroupIdentifier.From(resourceGroupName), registryName);
        }
    }

    private void HandleGetSingleRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier,
        string registryName)
    {
        var operation = _controlPlane.Get(subscriptionIdentifier, resourceGroupIdentifier, registryName);
        if (operation.Result == OperationResult.NotFound)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.ResourceNotFoundCode, registryName, resourceGroupIdentifier.Value);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource!.ToString());
    }

    private void HandleListByResourceGroupRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier)
    {
        var operation = _controlPlane.ListByResourceGroup(subscriptionIdentifier, resourceGroupIdentifier);
        var result = new ListContainerRegistriesResponse
        {
            Value = operation.Resource!.Select(ListContainerRegistriesResponse.ContainerRegistry.From).ToArray()
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.ToString());
    }

    private void HandleListBySubscriptionRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier)
    {
        var operation = _controlPlane.ListBySubscription(subscriptionIdentifier);
        var result = new ListContainerRegistriesResponse
        {
            Value = operation.Resource!.Select(ListContainerRegistriesResponse.ContainerRegistry.From).ToArray()
        };

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(result.ToString());
    }

    private void HandleDeleteRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier,
        string path)
    {
        var resourceGroupName = path.ExtractValueFromPath(4);
        var registryName = path.ExtractValueFromPath(8);

        if (string.IsNullOrWhiteSpace(registryName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var operation = _controlPlane.Delete(
            subscriptionIdentifier,
            ResourceGroupIdentifier.From(resourceGroupName),
            registryName);

        response.StatusCode = operation.Result == OperationResult.NotFound
            ? HttpStatusCode.NoContent
            : HttpStatusCode.OK;

        if (operation.Resource != null)
            response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCheckNameAvailabilityRequest(HttpContext context, HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier)
    {
        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var body = JsonSerializer.Deserialize<CheckNameAvailabilityRequest>(content, GlobalSettings.JsonOptions);

        if (body?.Name == null)
        {
            response.StatusCode = HttpStatusCode.BadRequest;
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
    }

    private sealed class CheckNameAvailabilityRequest
    {
        public string? Name { get; init; }
        public string? Type { get; init; }
    }
}
