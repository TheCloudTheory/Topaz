using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Queue;

internal sealed class ListServiceBusQueueAuthorizationRulesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/queues/{queueName}/authorizationRules"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/queues/authorizationRules/read"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var ns = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var queueName = context.Request.Path.Value.ExtractValueFromPath(10)!;
        var operation = _controlPlane.ListQueueAuthorizationRules(sub, rg, ns, queueName);
        response.CreateJsonContentResponse(ListServiceBusAuthorizationRulesResponse.From(operation.Resource ?? []));
    }
}
