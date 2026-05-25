using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.DataPlane;

internal sealed class GetEntityDataPlaneEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["GET /{entity}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/queues/read", "Microsoft.ServiceBus/namespaces/topics/read"];

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
        var entityType = _controlPlane.GetEntityType(subscriptionId!, resourceGroupId!, namespaceName, entityName!, string.Empty);

        switch (entityType)
        {
            case ServiceBusEntityType.Queue:
                var queueOperation = _controlPlane.GetQueue(subscriptionId!, resourceGroupId!, namespaceName, entityName!);
                if (queueOperation.Result == OperationResult.NotFound || queueOperation.Resource == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                }
                response.Content = new StringContent(queueOperation.Resource.ToXmlString());
                response.StatusCode = HttpStatusCode.OK;
                break;
            case ServiceBusEntityType.Topic:
                var topicOperation = _controlPlane.GetTopic(subscriptionId!, resourceGroupId!, namespaceName, entityName!);
                if (topicOperation.Result == OperationResult.NotFound || topicOperation.Resource == null)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    return;
                }
                response.Content = new StringContent(topicOperation.Resource.ToXmlString());
                response.StatusCode = HttpStatusCode.OK;
                break;
            default:
                response.StatusCode = HttpStatusCode.NotFound;
                break;
        }
    }
}
