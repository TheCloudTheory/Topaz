using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.ServiceBus.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.ServiceBus.Endpoints.Topic;

internal sealed class ListServiceBusTopicAuthorizationRulesEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}/authorizationRules"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/authorizationRules/read"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var ns = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(10)!;
        var operation = _controlPlane.ListTopicAuthorizationRules(sub, rg, ns, topicName);
        response.CreateJsonContentResponse(ListServiceBusAuthorizationRulesResponse.From(operation.Resource ?? []));
    }
}
