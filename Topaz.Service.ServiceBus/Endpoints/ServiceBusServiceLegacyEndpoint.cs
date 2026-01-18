using System.Net;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints;

public sealed class ServiceBusServiceLegacyEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = new(new ServiceBusResourceProvider(logger), logger);
    
    public string[] Endpoints =>
    [
        // When using MassTransit, the actual endpoint used comes from the actual FQDN of the namespaces,
        // ergo it's not leveraging the standard Azure Resource Manager endpoints to manage entities.
        "PUT /{entity}",
        "GET /{entity}",
        "GET /{entity}/{messageType}",
        "PUT /{entity}/{messageType}",
        "PUT /{entity}/{messageType}/Subscriptions/{subscription}"
    ];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.AmqpTlsConnectionPort], Protocol.Https);

    public HttpResponseMessage GetResponse(string path, string method, Stream input, IHeaderDictionary headers, QueryString query,
        GlobalOptions options)
    {
        var response = new HttpResponseMessage();

        try
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(GetResponse), "Handling request via an additional resource endpoint.");
                
            // SDK of any kind is expected to send a Host header with the following structure:
            // [namespace].servicebus.topaz.local.dev:[port]
            // so we just fetch the name of the namespace from it.
            var namespaceIdentifierFromHeader = headers["Host"].ToString().Split(".")[0];

            // The incoming requests may use either /{entity} or /{entity}/{messageType} endpoints.
            // If it's the latter then we assume it's a topic-related request
            var isTopicRequest = path.Split("/").Length == 3;

            if (isTopicRequest)
            {
                HandleTopicRequests(path, method, input, response, ServiceBusNamespaceIdentifier.From(namespaceIdentifierFromHeader));
                return response;
            }
            
            // If the requested endpoints contains additional segments,
            // it will be considered to be a subscription request.
            var isSubscriptionRequest = path.Split("/").Length > 3;

            if (isSubscriptionRequest)
            {
                HandleSubscriptionRequest(path, method, input, response, ServiceBusNamespaceIdentifier.From(namespaceIdentifierFromHeader));
                return response;
            }
            
            HandleQueueRequests(path, method, input, response, ServiceBusNamespaceIdentifier.From(namespaceIdentifierFromHeader));
            
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, ex.Message);
        }
        
        return response;
    }

    private void HandleSubscriptionRequest(string path, string method, Stream input, HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        var subscriptionName = path.ExtractValueFromPath(4);
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleSubscriptionRequest), "Extracted subscription name equal to `{0}`", subscriptionName);
        
        switch (method)
        {
            case "GET":
                HandleGetSubscriptionRequest(response, namespaceIdentifier, subscriptionName!);
                break;
            case "PUT":
                HandleCreateOrUpdateSubscriptionRequest(response, namespaceIdentifier, subscriptionName!, input);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }

    private void HandleCreateOrUpdateSubscriptionRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string subscriptionName, Stream input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "Executing for {0}/{1}.", namespaceIdentifier, subscriptionName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, subscriptionName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        using var reader = new StreamReader(input);
        var content = reader.ReadToEnd();
        
        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest), "Received payload: {0}", content);
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusQueueAtomRequest));
        using var stringReader = new StringReader(content);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusQueueAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateSubscriptionRequest),
            "Request properties: EnableExpress={0}, EnablePartitioning={1}, MaxSizeInMegabytes={2}",
            request.Content?.Properties?.EnableExpress, request.Content?.Properties?.EnablePartitioning,
            request.Content?.Properties?.MaxSizeInMegabytes);
        
        var operation = _controlPlane.CreateOrUpdateSubscription(identifiersOperation.subscriptionIdentifier!,
            identifiersOperation.resourceGroupIdentifier!, namespaceIdentifier, subscriptionName, CreateOrUpdateServiceBusQueueRequest.From(request));
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
        
        response.StatusCode = HttpStatusCode.OK;
    }

    private void HandleQueueRequests(string path, string method, Stream input, HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier)
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
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
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

    private void HandleCreateOrUpdateQueueRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string queueName, Stream input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateQueueRequest), "Executing for {0}/{1}.", namespaceIdentifier, queueName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateQueueRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, queueName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        using var reader = new StreamReader(input);
        var content = reader.ReadToEnd();
        
        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateQueueRequest), "Received payload: {0}", content);
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusQueueAtomRequest));
        using var stringReader = new StringReader(content);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusQueueAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateQueueRequest),
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

    private void HandleTopicRequests(string path, string method, Stream input, HttpResponseMessage response,
        ServiceBusNamespaceIdentifier namespaceIdentifier)
    {
        // Topic name comes in a form of {entity}/{messageType} when MassTransit creates the topology.
        var topicName = $"{path.ExtractValueFromPath(1)}/{path.ExtractValueFromPath(2)}";
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleTopicRequests), "Extracted topic name equal to `{0}`", topicName);
                
        switch (method)
        {
            case "GET":
                HandleGetTopicRequest(response, namespaceIdentifier, topicName);
                break;
            case "PUT":
                HandleCreateOrUpdateTopicRequest(response, namespaceIdentifier, topicName, input);
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }

    private void HandleCreateOrUpdateTopicRequest(HttpResponseMessage response, ServiceBusNamespaceIdentifier namespaceIdentifier, string topicName, Stream input)
    {
        logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateTopicRequest), "Executing for {0}/{1}.", namespaceIdentifier, topicName);
        
        var identifiersOperation = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceIdentifier);
        if (identifiersOperation.result == OperationResult.NotFound)
        {
            logger.LogDebug(nameof(ServiceBusServiceEndpoint), nameof(HandleCreateOrUpdateTopicRequest), "No identifier found for {0}/{1}.", namespaceIdentifier, topicName);
            
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }
        
        using var reader = new StreamReader(input);
        var content = reader.ReadToEnd();
        
        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateTopicRequest), "Received payload: {0}", content);
        
        var serializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusTopicAtomRequest));
        using var stringReader = new StringReader(content);

        if (serializer.Deserialize(stringReader) is not CreateOrUpdateServiceBusTopicAtomRequest request)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        logger.LogDebug(nameof(ServiceBusServiceLegacyEndpoint), nameof(HandleCreateOrUpdateTopicRequest),
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