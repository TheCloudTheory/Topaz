using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.EventPipeline;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Service.Subscription.Models.Responses;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.KeyVault.Endpoints;

internal sealed class ListKeyVaultsBySubscriptionEndpoint(Pipeline eventPipeline, ITopazLogger logger)
    : IEndpointDefinition
{
    private readonly KeyVaultControlPlane _controlPlane = KeyVaultControlPlane.New(eventPipeline, logger);

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.KeyVault/vaults"
    ];

    public string[] Permissions => ["Microsoft.KeyVault/vaults/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListKeyVaultsBySubscriptionEndpoint), nameof(GetResponse), "Executing {0}.",
            nameof(GetResponse));

        var subscriptionIdentifier = SubscriptionIdentifier.From(context.Request.Path.Value!.ExtractValueFromPath(2));

        var keyVaults = _controlPlane.ListBySubscription(subscriptionIdentifier);
        if (keyVaults.Result != OperationResult.Success || keyVaults.Resource == null)
        {
            response.StatusCode = HttpStatusCode.InternalServerError;
            return;
        }

        var result = new ListSubscriptionResourcesResponse
        {
            Value = keyVaults.Resource.Select(ListSubscriptionResourcesResponse.GenericResourceExpanded.From).ToArray()
        };

        response.CreateJsonContentResponse(result);
    }
}
