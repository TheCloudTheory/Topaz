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
    private readonly ServiceBusServiceControlPlane _controlPlane = new(new ResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}",
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}",
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}"
    ];
    public (int Port, Protocol Protocol) PortAndProtocol => (GlobalSettings.DefaultResourceManagerPort, Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        logger.LogDebug($"Executing {nameof(GetResponse)}: [{method}] {path}{query}");
        
        var response = new HttpResponseMessage();

        try
        {
            var subscriptionIdentifier = SubscriptionIdentifier.From(path.ExtractValueFromPath(2));
            var resourceGroupIdentifier = ResourceGroupIdentifier.From(path.ExtractValueFromPath(4));
            var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(path.ExtractValueFromPath(8));
            var queueName = path.ExtractValueFromPath(10);

            if (string.IsNullOrWhiteSpace(queueName) == false)
            {
                switch (method)
                {
                    case "PUT":
                        HandleCreateOrUpdateQueue(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, queueName, input);
                        break;
                    case "GET":
                        HandleGetQueue(response, namespaceIdentifier, queueName);
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
                        HandleCreateOrUpdateNamespace(response, subscriptionIdentifier, resourceGroupIdentifier, namespaceIdentifier, input);
                        break;
                    case "GET":
                        HandleGetNamespace(response, namespaceIdentifier);
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

    private void HandleGetQueue(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName)
    {
        var operation = _controlPlane.GetQueue(namespaceIdentifier, queueName);
        if (operation.result == OperationResult.NotFound || operation.resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.resource.ToString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateQueue(HttpResponseMessage response, SubscriptionIdentifier subscriptionIdentifier,
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
        if (operation.result != OperationResult.Created && operation.result != OperationResult.Updated ||
            operation.resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content = new StringContent(operation.resource.ToString());
    }

    private void HandleGetNamespace(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var operation = _controlPlane.GetNamespace(namespaceIdentifier);
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
        if (operation.result != OperationResult.Created && operation.result != OperationResult.Updated || operation.resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }
        
        response.StatusCode = operation.result == OperationResult.Created ? HttpStatusCode.Created : HttpStatusCode.OK;
        response.Content = new StringContent(operation.resource.ToString());
    }
}