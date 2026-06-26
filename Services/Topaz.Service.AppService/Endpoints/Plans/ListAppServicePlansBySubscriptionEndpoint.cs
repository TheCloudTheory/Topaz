using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.AppService.Endpoints.Plans;

internal sealed class ListAppServicePlansBySubscriptionEndpoint(ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly AppServicePlanControlPlane _controlPlane = AppServicePlanControlPlane.New(logger);

    public string? ProviderNamespace => "Microsoft.Web";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Web/serverfarms"
    ];

    public string[] Permissions => ["Microsoft.Web/serverfarms/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListAppServicePlansBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(GetResponse));

        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var plans = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (plans.Result != OperationResult.Success || plans.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = plans.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From!).ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
