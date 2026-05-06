using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Requests;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class CreateOrUpdateServiceBusTopicEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceName = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(10);

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var request = JsonSerializer.Deserialize<CreateOrUpdateServiceBusTopicRequest>(content,
            GlobalSettings.JsonOptions) ?? new CreateOrUpdateServiceBusTopicRequest();

        var operation = _controlPlane.CreateOrUpdateTopic(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceName, topicName!, request);

        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.StatusCode = operation.Result == OperationResult.Created
            ? HttpStatusCode.Created
            : HttpStatusCode.OK;
        response.CreateJsonContentResponse(operation.Resource);
    }
}
