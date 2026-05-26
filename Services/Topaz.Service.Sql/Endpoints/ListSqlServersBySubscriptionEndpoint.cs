using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.Sql.Endpoints;

internal sealed class ListSqlServersBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly SqlServiceControlPlane _controlPlane = SqlServiceControlPlane.New(eventPipeline, logger);

    public string? ProviderNamespace => "Microsoft.Sql";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Sql/servers"
    ];

    public string[] Permissions => ["Microsoft.Sql/servers/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListSqlServersBySubscriptionEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        var subscriptionIdentifier =
            SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var servers = _controlPlane.ListBySubscription(subscriptionIdentifier);

        var result = new ListSubscriptionResourcesResponse
        {
            Value = servers
                .Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!)
                .ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
