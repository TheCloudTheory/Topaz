using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.LogAnalytics.Endpoints;

internal sealed class ListWorkspacesBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly LogAnalyticsServiceControlPlane _controlPlane =
        LogAnalyticsServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.OperationalInsights";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.OperationalInsights/workspaces"
    ];

    public string[] Permissions => ["Microsoft.OperationalInsights/workspaces/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var result = _controlPlane.ListBySubscription(sub);
        if (result.Result != OperationResult.Success || result.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var list = new ListSubscriptionResourcesResponse
        {
            Value = result.Resource
                .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!)
                .ToArray()
        };

        response.CreateJsonContentResponse(list);
    }
}
