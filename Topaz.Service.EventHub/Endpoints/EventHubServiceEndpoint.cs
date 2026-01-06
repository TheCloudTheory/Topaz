using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.EventHub.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.EventHub.Endpoints;

public sealed class EventHubServiceEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly EventHubServiceControlPlane _controlPlane = new(new ResourceProvider(logger), logger);
    public string[] Endpoints => [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventHub/namespaces/{namespaceName}/eventhubs/{eventHubName}"
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);
    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query, GlobalOptions options, Guid correlationId)
    {
        logger.LogDebug(nameof(EventHubServiceEndpoint), nameof(GetResponse), "Handling `[{0}]{1}`", correlationId, method, path);

        var response = new HttpResponseMessage();

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var namespaceIdentifier = EventHubNamespaceIdentifier.From(path.ExtractValueFromPath(8));
            var hubName = path.ExtractValueFromPath(10);
            
            switch (method)
            {
                case "GET":
                {
                    HandleGetNamespaceRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
                    break;
                }
                case "PUT":
                {
                    if (!string.IsNullOrWhiteSpace(hubName))
                    {
                        HandleCreateOrUpdateEventHubRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, hubName, input);
                        break;
                    }
                    
                    HandleCreateOrUpdateEventHubNamespaceRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, input);
                    break;
                }
                case "DELETE":
                    HandleDeleteEventHubNamespaceRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
                    break;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex);

            response.Content = new StringContent(ex.Message);
            response.StatusCode = HttpStatusCode.InternalServerError;
        }

        return response;
    }

    private void HandleDeleteEventHubNamespaceRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier, EventHubNamespaceIdentifier namespaceIdentifier)
    {
        logger.LogDebug($"Executing {nameof(HandleDeleteEventHubNamespaceRequest)}.");
        
        var existingNamespace = _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        switch (existingNamespace.Result)
        {
            case OperationResult.NotFound:
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            case OperationResult.Failed:
                response.StatusCode = HttpStatusCode.InternalServerError;
                return;
            default:
                _controlPlane.DeleteNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
                response.StatusCode = HttpStatusCode.OK;
                break;
        }
    }

    private void HandleCreateOrUpdateEventHubNamespaceRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        EventHubNamespaceIdentifier namespaceIdentifier, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateEventHubNamespaceRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _controlPlane.CreateOrUpdateNamespace(subscriptionIdentifier, resourceGroupIdentifier, request.Location!, namespaceIdentifier, request);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleCreateOrUpdateEventHubRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, EventHubNamespaceIdentifier namespaceIdentifier,
        string queueName, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateEventHubRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateEventHub(subscriptionIdentifier, resourceGroupIdentifier, @namespaceIdentifier, queueName, request);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }

    private void HandleGetNamespaceRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, EventHubNamespaceIdentifier namespaceIdentifier)
    {
        var operation = _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }
}