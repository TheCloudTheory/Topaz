using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Queue;

internal sealed class DeleteServiceBusQueueAuthorizationRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}/authorizationRules/{authorizationRuleName}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/queues/authorizationRules/delete"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var ns = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var queueName = context.Request.Path.Value.ExtractValueFromPath(10)!;
        var ruleName = context.Request.Path.Value.ExtractValueFromPath(12)!;
        var operation = _controlPlane.DeleteQueueAuthorizationRule(sub, rg, ns, queueName, ruleName);
        response.StatusCode = operation.Result == OperationResult.NotFound ? HttpStatusCode.NotFound : HttpStatusCode.OK;
    }
}
