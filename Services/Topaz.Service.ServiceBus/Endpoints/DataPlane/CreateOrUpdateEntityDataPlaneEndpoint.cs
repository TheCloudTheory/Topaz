using System.Net;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.DataPlane;

internal sealed class CreateOrUpdateEntityDataPlaneEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["PUT /{entity}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/queues/write", "Microsoft.ServiceBus/namespaces/topics/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.AdditionalServiceBusPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var namespaceName = ServiceBusNamespaceIdentifier.From(context.Request.Headers["Host"].ToString().Split(".")[0]);
        var (result, subscriptionId, resourceGroupId) = ServiceBusServiceControlPlane.GetIdentifiersForParentResource(namespaceName);
        if (result == OperationResult.NotFound)
        {
            response.StatusCode = HttpStatusCode.NotFound;
            return;
        }

        var entityName = context.Request.Path.Value.ExtractValueFromPath(1);
        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var entityType = _controlPlane.GetEntityType(subscriptionId!, resourceGroupId!, namespaceName, entityName!, content);

        switch (entityType)
        {
            case ServiceBusEntityType.Queue:
                var queueSerializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusQueueAtomRequest));
                if (queueSerializer.Deserialize(new StringReader(content)) is not CreateOrUpdateServiceBusQueueAtomRequest queueAtomRequest)
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                }
                var queueOperation = _controlPlane.CreateOrUpdateQueue(subscriptionId!, resourceGroupId!, namespaceName, entityName!, CreateOrUpdateServiceBusQueueRequest.From(queueAtomRequest));
                if (queueOperation.Result != OperationResult.Created && queueOperation.Result != OperationResult.Updated || queueOperation.Resource == null)
                {
                    response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, "Unknown error when performing CreateOrUpdate operation.");
                    return;
                }
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(queueOperation.Resource.ToXmlString());
                break;
            case ServiceBusEntityType.Topic:
                var topicSerializer = new XmlSerializer(typeof(CreateOrUpdateServiceBusTopicAtomRequest));
                if (topicSerializer.Deserialize(new StringReader(content)) is not CreateOrUpdateServiceBusTopicAtomRequest topicAtomRequest)
                {
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    return;
                }
                var topicOperation = _controlPlane.CreateOrUpdateTopic(subscriptionId!, resourceGroupId!, namespaceName, entityName!, CreateOrUpdateServiceBusTopicRequest.From(topicAtomRequest));
                if (topicOperation.Result != OperationResult.Created && topicOperation.Result != OperationResult.Updated || topicOperation.Resource == null)
                {
                    response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode, "Unknown error when performing CreateOrUpdate operation.");
                    return;
                }
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new StringContent(topicOperation.Resource.ToXmlString());
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }
}
