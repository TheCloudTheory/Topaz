using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.FinOps.Endpoints;

/// <summary>
/// Returns estimated monthly costs for all resources in a Topaz-emulated subscription.
/// Topaz-native route — not an ARM provider endpoint.
/// </summary>
internal sealed class GetEstimatedCostsEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    private readonly ResourceInventoryCollector _inventoryCollector = new(logger);
    private readonly CostEstimationService _costEstimationService = new();

    public string? ProviderNamespace => null;

    public string[] Endpoints => ["GET /topaz/subscriptions/{subscriptionId}/estimatedCosts"];

    public string[] Permissions => [];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        var subscriptionId = context.Request.Path.Value.ExtractValueFromPath(3);
        if (string.IsNullOrEmpty(subscriptionId))
        {
            response.StatusCode = HttpStatusCode.BadRequest;
            return;
        }

        var currency = context.Request.Query["currency"].FirstOrDefault() ?? "USD";

        logger.LogDebug(
            nameof(GetEstimatedCostsEndpoint),
            nameof(GetResponse),
            "Estimating costs for subscription {0} in currency {1}.",
            subscriptionId,
            currency);

        var changes = _inventoryCollector.CollectForSubscription(subscriptionId);

        logger.LogDebug(
            nameof(GetEstimatedCostsEndpoint),
            nameof(GetResponse),
            "Found {0} resources in subscription {1}.",
            changes.Length,
            subscriptionId);

        var result = _costEstimationService.EstimateAsync(subscriptionId, changes, currency).GetAwaiter().GetResult();

        response.CreateJsonContentResponse(result);
    }
}
