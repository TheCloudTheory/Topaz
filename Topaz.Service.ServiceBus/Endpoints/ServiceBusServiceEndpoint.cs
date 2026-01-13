using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus.Endpoints;

public sealed class ServiceBusServiceEndpoint(ITopazLogger logger)  : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = new(new ServiceBusResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}"
    ];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => (
    [
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.AdditionalResourceManagerPort,
        GlobalSettings.AmqpTlsConnectionPort
    ], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers,
        QueryString query,
        GlobalOptions options, Guid correlationId)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(path.ExtractValueFromPath(8));
            var queueOrTopicName = path.ExtractValueFromPath(10);
            var isQueueRequest = path.Contains("/queues");

            if (!string.IsNullOrWhiteSpace(queueOrTopicName))
            {
                if (isQueueRequest)
                {
                    switch (method)
                    {
                        case "PUT":
                            HandleCreateOrUpdateQueueRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueOrTopicName, input);
                            break;
                        case "GET":
                            HandleGetQueueRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueOrTopicName);
                            break;
                        default:
                            response.StatusCode = HttpStatusCode.NotFound;
                            break;
                    }
                }
                else
                {
                    switch (method)
                    {
                        case "PUT":
                            HandleCreateOrUpdateTopicRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueOrTopicName, input);
                            break;
                        case "GET":
                            HandleGetTopicRequest(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueOrTopicName);
                            break;
                        default:
                            response.StatusCode = HttpStatusCode.NotFound;
                            break;
                    }
                }
            }
            else
            {
                switch (method)
                {
                    case "PUT":
                        HandleCreateOrUpdateNamespace(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, input);
                        break;
                    case "GET":
                        HandleGetNamespace(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
                        break;
                    default:
                        response.StatusCode = HttpStatusCode.NotFound;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, ex.Message);
        }

        return response;
    }

    private void HandleCreateOrUpdateTopicRequest(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateServiceBusTopicRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateTopic(subscriptionIdentifier, resourceGroupIdentifier, @namespaceIdentifier, topicName, request);
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

    private void HandleGetTopicRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string topicName)
    {
        var operation = _controlPlane.GetTopic(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, topicName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetQueueRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName)
    {
        var operation = _controlPlane.GetQueue(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateQueueRequest(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier,
        string queueName, Stream input)
    {
        using var reader = new StreamReader(input);

        var content = reader.ReadToEnd();
        var request =
            JsonSerializer.Deserialize<CreateOrUpdateServiceBusQueueRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var operation = _controlPlane.CreateOrUpdateQueue(subscriptionIdentifier, resourceGroupIdentifier, @namespaceIdentifier, queueName, request);
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

    private void HandleGetNamespace(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
        ResourceGroupIdentifier resourceGroupIdentifier, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var operation = _controlPlane.GetNamespace(subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier);
        if (operation.result == OperationResult.NotFound || operation.resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateNamespace(HttpResponseMessage response,
        SubscriptionIdentifier subscriptionIdentifier, ResourceGroupIdentifier resourceGroupIdentifier,
        ServiceBusNamespaceIdentifier namespaceIdentifier, Stream input)
    {
        using var reader = new StreamReader(input);
        
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateServiceBusNamespaceRequest>(content, GlobalSettings.JsonOptions);

        if (request == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }
        
        var operation = _controlPlane.CreateOrUpdateNamespace(subscriptionIdentifier, resourceGroupIdentifier, request.Location!, @namespaceIdentifier, request);
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated || operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }
        
        response.StatusCode = operation.Result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(operation.Resource.ToString());
    }
}