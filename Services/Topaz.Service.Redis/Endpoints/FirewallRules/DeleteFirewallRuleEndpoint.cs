using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Redis.Endpoints.FirewallRules;

internal sealed class DeleteFirewallRuleEndpoint(Pipeline eventPipeline, ITopazLogger logger) : IEndpointDefinition
{
    private readonly RedisServiceControlPlane _controlPlane =
        RedisServiceControlPlane.New(eventPipeline, logger);

    public string ProviderNamespace => "Microsoft.Cache";

    public string[] Endpoints =>
    [
        "DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Cache/redis/{cacheName}/firewallRules/{ruleName}"
    ];

    public string[] Permissions => ["Microsoft.Cache/redis/firewallRules/delete"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(2));
        var rg = ResourceGroupIdentifier.From(context.Request.Path.Value.ExtractValueFromPath(4));
        var name = context.Request.Path.Value.ExtractValueFromPath(8);
        var ruleName = context.Request.Path.Value.ExtractValueFromPath(10);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ruleName))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var result = _controlPlane.DeleteRule(sub, rg, name, ruleName);
        if (result.Result != OperationResult.Deleted)
        {
            response.CreateErrorResponse(result.Code!, result.Reason!);
            return;
        }

        response.StatusCode = HttpStatusCode.OK;
    }
} 