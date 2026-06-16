using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class GetServiceBusRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/rules/read"];

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

        var operation = _controlPlane.GetRule(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceName, topicName, subscriptionName, ruleName);

        if (operation.Result == OperationResult.NotFound || operation.Resource == null)
        {
            response.CreateErrorResponse(operation.Code!, operation.Reason!, operation.Result);
            return;
        }

        response.CreateJsonContentResponse(operation.Resource);
    }
}
