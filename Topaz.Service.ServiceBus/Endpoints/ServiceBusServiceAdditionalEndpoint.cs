using System.Net;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints;

public sealed class ServiceBusServiceAdditionalEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = new(new ServiceBusResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        // When using MassTransit, the actual endpoint used comes from the actual FQDN of the namespaces,
        // ergo it's not leveraging the standard Azure Resource Manager endpoints to manage entities.
        "PUT /{entity}",
        "GET /{entity}",
        "DELETE /{entity}",
        "GET /{entity}/{messageType}",
        "PUT /{entity}/{messageType}",
        "GET /{entity}/Subscriptions/{subscription}",
        "PUT /{entity}/Subscriptions/{subscription}",
        "DELETE /{entity}/Subscriptions/{subscription}",
        "PUT /{entity}/{messageType}/Subscriptions/{subscription}"
    ];

    public string[] Permissions => ["*"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.AmqpTlsConnectionPort, GlobalSettings.AdditionalServiceBusPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        try
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(GetResponse), "Handling a request via an additional resource endpoint.");
            
            // SDK of any kind is expected to send a Host header with the following structure:
            // [namespace].servicebus.topaz.local.dev:[port]
            // so we just fetch the name of the namespace from it.
            var namespaceIdentifierFromHeader = context.Request.Headers["Host"].ToString().Split(".")[0];
            var namespaceIdentifier = ServiceBusNamespaceIdentifier.From(namespaceIdentifierFromHeader);
            
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(GetResponse), "Handling a request for `{0}` namespace.", namespaceIdentifier);
            
            // Requests coming in for the additional endpoint doesn't have a clear indicator of for 
            // what kind of entity they are sent for. As the SDK sends a request for an entity,
            // we will make the assumption that if an entity exists, such a type of the entity
            // should be handled.
            var entityName = context.Request.Path.Value.Contains("/Subscriptions")
                ? GetSubscriptionNameFromPath(context.Request.Path.Value)
                : context.Request.Path.Value.ExtractValueFromPath(1);
            var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
            if (identifiersOperation.result == OperationResult.NotFound)
            {
                logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, entityName);
            
                response.StatusCode = HttpStatusCode.NotFound;
                return;
            }
            
            // We will read the content early from the request as the payload will help us
            // understand what kind of entity is going to be handled.
            using var reader = new StreamReader(context.Request.Body);
            var content = reader.ReadToEnd();
        
            logger.LogDebug(nameof(ServiceBusServiceAdditionalEndpoint), nameof(GetResponse), "Received payload: {0}", content);
            
            var entityType = _controlPlane.GetEntityType(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, entityName!, content);
            logger.LogDebug(nameof(ServiceBusServiceAdditionalEndpoint), nameof(GetResponse), "Detected entity type: `{0}`", entityType);
            switch (entityType)
            {
                case ServiceBusEntityType.Topic:
                    HandleTopicRequests(context.Request.Path.Value, context.Request.Method, content, response, namespaceIdentifier);
                    return;
                case ServiceBusEntityType.Subscription:
                    HandleSubscriptionRequest(context.Request.Path.Value, context.Request.Method, content, response, namespaceIdentifier);
                    return;
                case ServiceBusEntityType.Queue:
                    HandleQueueRequests(context.Request.Path.Value, context.Request.Method, content, response, namespaceIdentifier);
                    return;
                default:
                    response.StatusCode = HttpStatusCode.NotFound;
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, ex.Message);
        }
    }

    private static string? GetSubscriptionNameFromPath(string path)
    {
        return string.IsNullOrWhiteSpace(path.ExtractValueFromPath(4)) ? path.ExtractValueFromPath(3) : path.ExtractValueFromPath(4);
    }

    private void HandleSubscriptionRequest(string path, string method, string input, HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var subscriptionName = GetSubscriptionNameFromPath(path);
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleSubscriptionRequest), "Extracted subscription name equal to `{0}`", subscriptionName);
        
        switch (method)
        {
            case "GET":
                HandleGetSubscriptionRequest(response, namespaceIdentifier, subscriptionName!);
                break;
            case "PUT":
                HandleCreateOrUpdateSubscriptionRequest(response, namespaceIdentifier, subscriptionName!, input);
                break;
            case "DELETE":
                HandleDeleteSubscriptionRequest(response, namespaceIdentifier, subscriptionName!);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }

    private void HandleDeleteSubscriptionRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleDeleteSubscriptionRequest), "Executing for {0}/{1}.", namespaceIdentifier, subscriptionName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.DeleteSubscription(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, subscriptionName);
        if (operation == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateSubscriptionRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName, string input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "Executing for {0}/{1}.", namespaceIdentifier, subscriptionName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, subscriptionName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusSubscriptionAtomRequest));
        using var stringReader = new StringReader(input);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusSubscriptionAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceAdditionalEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest),
            "Request properties: Status={0}, MaxDeliveryCount={1}, LockDuration={2}",
            request.Content?.Properties?.Status, request.Content?.Properties?.MaxDeliveryCount,
            request.Content?.Properties?.LockDuration);
        
        var operation = _controlPlane.CreateOrUpdateSubscription(identifiersOperation.subscriptionIdentifier!,
            identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, subscriptionName, CreateOrUpdateServiceBusSubscriptionRequest.From(request));
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content =
            new StringContent(operation.Resource.ToXmlString());
    }

    private void HandleGetSubscriptionRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleGetSubscriptionRequest), "Executing for {0}/{1}.", namespaceIdentifier, subscriptionName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.GetSubscription(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, subscriptionName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToXmlString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleQueueRequests(string path, string method, string input, HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var queueName = path.ExtractValueFromPath(1);
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleQueueRequests), "Extracted queue name equal to `{0}`", queueName);
                
        switch (method)
        {
            case "GET":
                HandleGetQueueRequest(response, namespaceIdentifier, queueName!);
                break;
            case "PUT":
                HandleCreateOrUpdateQueueRequest(response, namespaceIdentifier, queueName!, input);
                break;
            case "DELETE":
                HandleDeleteQueueRequest(response, namespaceIdentifier, queueName!);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }

    private void HandleDeleteQueueRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleDeleteQueueRequest), "Executing for {0}/{1}.", namespaceIdentifier, queueName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.DeleteQueue(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, queueName);
        if (operation == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleGetQueueRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleGetQueueRequest), "Executing for {0}/{1}.", namespaceIdentifier, queueName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.GetQueue(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, queueName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToXmlString());
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateQueueRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName, string input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateQueueRequest), "Executing for {0}/{1}.", namespaceIdentifier, queueName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateQueueRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, queueName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusQueueAtomRequest));
        using var stringReader = new StringReader(input);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusQueueAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceAdditionalEndpoint), nameof(HandleCreateOrUpdateQueueRequest),
            "Request properties: EnableExpress={0}, EnablePartitioning={1}, MaxSizeInMegabytes={2}",
            request.Content?.Properties?.EnableExpress, request.Content?.Properties?.EnablePartitioning,
            request.Content?.Properties?.MaxSizeInMegabytes);
        
        var operation = _controlPlane.CreateOrUpdateQueue(identifiersOperation.subscriptionIdentifier!,
            identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, queueName, CreateOrUpdateServiceBusQueueRequest.From(request));
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content =
            new StringContent(operation.Resource.ToXmlString());
    }

    private void HandleTopicRequests(string path, string method, string input, HttpResponseMessage response,
        ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var topicName = path.TrimStart('/');
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleTopicRequests), "Extracted topic name equal to `{0}`", topicName);
                
        switch (method)
        {
            case "GET":
                HandleGetTopicRequest(response, namespaceIdentifier, topicName);
                break;
            case "PUT":
                HandleCreateOrUpdateTopicRequest(response, namespaceIdentifier, topicName, input);
                break;
            case "DELETE":
                HandleDeleteTopicRequest(response, namespaceIdentifier, topicName);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }

    private void HandleDeleteTopicRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleDeleteQueueRequest), "Executing for {0}/{1}.", namespaceIdentifier, topicName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.DeleteTopic(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, topicName);
        if (operation == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleCreateOrUpdateTopicRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, string input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateTopicRequest), "Executing for {0}/{1}.", namespaceIdentifier, topicName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateTopicRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, topicName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusTopicAtomRequest));
        using var stringReader = new StringReader(input);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusTopicAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceAdditionalEndpoint), nameof(HandleCreateOrUpdateTopicRequest),
            "Request properties: EnableExpress={0}, EnablePartitioning={1}, MaxSizeInMegabytes={2}",
            request.Content?.Properties?.EnableExpress, request.Content?.Properties?.EnablePartitioning,
            request.Content?.Properties?.MaxSizeInMegabytes);
        
        var operation = _controlPlane.CreateOrUpdateTopic(identifiersOperation.subscriptionIdentifier!,
            identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, topicName, CreateOrUpdateServiceBusTopicRequest.From(request));
        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                $"Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
        response.Content =
            new StringContent(operation.Resource.ToXmlString());
    }

    private void HandleGetTopicRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleGetTopicRequest), "Executing for {0}/{1}.", namespaceIdentifier, topicName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        var operation = _controlPlane.GetTopic(identifiersOperation.subscriptionIdentifier!, identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, topicName);
        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        response.Content = new StringContent(operation.Resource.ToXmlString());
        response.StatusCode = HttpStatusCode.OK;
    }
}