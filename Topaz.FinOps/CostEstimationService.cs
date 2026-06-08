using ACE.Core;
using ACE.WhatIf;
using Topaz.FinOps.Models;

namespace Topaz.FinOps;

/// <summary>
/// Bridges the Topaz resource inventory with the ACE estimation engine and
/// produces an <see cref="EstimatedCostsResponse"/>.
/// </summary>
internal sealed class CostEstimationService
{
    private static readonly EstimationService AceEstimationService = new();

    public async Task<EstimatedCostsResponse> EstimateAsync(
        string subscriptionId,
        WhatIfChange[] changes,
        string currency = "USD",
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<CurrencyCode>(currency, ignoreCase: true, out var currencyCode))
        {
            currencyCode = CurrencyCode.USD;
        }

        var options = new CoreEstimationOptions { Currency = currencyCode };
        var output = await AceEstimationService.EstimateAsync(changes, options, null, cancellationToken);

        var resources = output.Resources
            .Select(r => new ResourceCostEntry
            {
                ResourceId = r.Id,
                ResourceType = ExtractTypeFromResourceId(r.Id),
                EstimatedMonthlyCost = r.TotalCost.OriginalValue
            })
            .ToList();

        return new EstimatedCostsResponse
        {
            SubscriptionId = subscriptionId,
            Currency = output.Currency,
            TotalMonthlyCost = output.TotalCost.OriginalValue,
            Resources = resources
        };
    }

    /// <summary>
    /// Extracts the resource type from an ARM resource ID.
    /// E.g. "/subscriptions/.../resourceGroups/.../providers/Microsoft.KeyVault/vaults/myVault"
    /// → "Microsoft.KeyVault/vaults"
    /// </summary>
    private static string ExtractTypeFromResourceId(string resourceId)
    {
        var parts = resourceId.Split('/');
        var providersIndex = Array.IndexOf(parts, "providers");
        if (providersIndex < 0 || providersIndex + 2 >= parts.Length)
        {
            return string.Empty;
        }

        return $"{parts[providersIndex + 1]}/{parts[providersIndex + 2]}";
    }
}
