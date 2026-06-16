using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class DeleteServiceBusRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane =
        ServiceBusServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.ServiceBus";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}/subscriptions/{subscriptionName}/rules/{ruleName}",
    ];

    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/subscriptions/rules/delete"];

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

        var result = _controlPlane.DeleteRule(subscriptionIdentifier, resourceGroupIdentifier,
            namespaceName, topicName, subscriptionName, ruleName);

        if (result == OperationResult.NotFound)
        {
            response.CreateErrorResponse("RuleNotFound", $"Rule '{ruleName}' not found.", result);
            return;
        }

        response.StatusCode = HttpStatusCode.NoContent;
    }
}
