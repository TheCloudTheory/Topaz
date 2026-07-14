using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Insights.Endpoints;

internal sealed class ListComponentsBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly ApplicationInsightsServiceControlPlane _controlPlane =
        ApplicationInsightsServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "microsoft.insights";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/microsoft.insights/components"
    ];

    public string[] Permissions => ["microsoft.insights/components/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var sub = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var result = _controlPlane.ListBySubscription(sub);
        var list = new ListSubscriptionResourcesResponse
        {
            Value = result.Resource!
                .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!)
                .ToArray()
        };

        response.CreateJsonContentResponse(list);
    }
}
