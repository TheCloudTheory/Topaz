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

internal sealed class CreateOrUpdateServiceBusTopicAuthorizationRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ServiceBusServiceControlPlane _controlPlane = ServiceBusServiceControlPlane.New(eventPipeline, logger);
    public string? ProviderNamespace => "Microsoft.ServiceBus";
    public string[] Endpoints => ["PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.ServiceBus/namespaces/{namespaceName}/topics/{topicName}/authorizationRules/{authorizationRuleName}"];
    public string[] Permissions => ["Microsoft.ServiceBus/namespaces/topics/authorizationRules/write"];
    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol => ([GlobalSettings.DefaultResourceManagerPort, GlobalSettings.HttpsPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var ns = ServiceBusNamespaceIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(8));
        var topicName = context.Request.Path.Value.ExtractValueFromPath(10)!;
        var ruleName = context.Request.Path.Value.ExtractValueFromPath(12)!;
        using var reader = new StreamReader(context.Request.Body);
        var request = JsonSerializer.Deserialize<CreateOrUpdateServiceBusAuthorizationRuleRequest>(reader.ReadToEnd(), GlobalSettings.JsonOptions) ?? new CreateOrUpdateServiceBusAuthorizationRuleRequest();
        var operation = _controlPlane.CreateOrUpdateTopicAuthorizationRule(sub, rg, ns, topicName, ruleName, request);
        response.CreateJsonContentResponse(operation.Resource!);
    }
}
