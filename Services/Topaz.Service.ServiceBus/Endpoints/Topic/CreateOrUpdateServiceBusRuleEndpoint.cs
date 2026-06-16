using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class CreateOrUpdateServiceBusRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";

    public string[] Endpoints =>
    [
        "PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/rules/write"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([
        GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort
    ], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var resourceGroupIdentifier = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var namespaceName = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(10)!;
        var subscriptionName = context.Request.Path.Value.ExtractValueFromPath(12)!;
        var ruleName = context.Request.Path.Value.ExtractValueFromPath(14)!;

        using var reader = new StreamReader(context.Request.Body);
        var content = reader.ReadToEnd();
        var properties = JsonSerializer.Deserialize<ServiceBusRuleResourceProperties>(content,
            GlobalSettings.JsonOptions) ?? ServiceBusRuleResourceProperties.DefaultTrueFilter();

        var operation = _controlPlane.CreateOrUpdateRule(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceName, topicName, subscriptionName, ruleName, properties);

        if (operation.Result != OperationResult.Created && operation.Result != OperationResult.Updated ||
            operation.Resource == null)
        {
            response.CreateErrorResponse(HttpResponseMessageExtensions.InternalErrorCode,
                "Unknown error when performing CreateOrUpdate operation.");
            return;
        }

        response.CreateJsonContentResponse(operation.Resource);
    }
}
