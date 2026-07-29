using System.Net;
using Microsoft.AspNetCore.Http;
using Topaz.Service.Shared;
using Topaz.Service.VirtualMachine.Models;
using Topaz.Shared;
using Topaz.Shared.Extensions;

namespace Topaz.Service.VirtualMachine.Endpoints;

/// <summary>
/// Returns a list of compute resource SKUs for the subscription, optionally filtered by location.
/// Used by tools such as ACE (Azure Cost Estimator) to determine VM capabilities (e.g. PremiumIO support).
/// </summary>
internal sealed class ListComputeResourceSkusEndpoint(ITopazLogger logger) : IEndpointDefinition
{
    // VM SKU catalogue with PremiumIO flag.
    // PremiumIO=true for any SKU whose name contains an 's' addendum per the Azure VM naming convention:
    // https://learn.microsoft.com/en-us/azure/virtual-machines/vm-naming-conventions

    public string? ProviderNamespace => "Microsoft.Compute";

    public string[] Endpoints =>
    [
        "GET /subscriptions/{subscriptionId}/providers/Microsoft.Compute/skus"
    ];

    public string[] Permissions => ["Microsoft.Compute/skus/read"];

    public (ushort[] Ports, Protocol Protocol) PortsAndProtocol =>
        ([GlobalSettings.DefaultResourceManagerPort], Protocol.Https);

    public void GetResponse(HttpContext context, HttpResponseMessage response, GlobalOptions options)
    {
        logger.LogDebug(nameof(ListComputeResourceSkusEndpoint), nameof(GetResponse),
            "Executing {0}.", nameof(GetResponse));

        try
        {
            // The $filter query param is "location eq 'eastus'" — extract the location value.
            var filter = context.Request.Query["$filter"].FirstOrDefault() ?? string.Empty;
            var location = ExtractLocationFromFilter(filter) ?? "eastus";

            var skus = ComputeResourceSkuProvider.KnownSkus
                .Select(s => ComputeResourceSkuEntry.ForVirtualMachine(s.Name, s.Tier, location, s.PremiumIo))
                .ToArray();

            response.CreateJsonContentResponse(new ComputeResourceSkuListResponse(skus));
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
            response.StatusCode = HttpStatusCode.InternalServerError;
            response.Content = new StringContent(ex.Message);
        }
    }

    /// <summary>
    /// Parses the location value from a filter string such as <c>location eq 'eastus'</c>.
    /// Returns null if the filter is absent or does not follow the expected pattern.
    /// </summary>
    private static string? ExtractLocationFromFilter(string filter)
    {
        // Expected format: "location eq 'eastus'"
        const string prefix = "location eq '";
        var start = filter.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;

        start += prefix.Length;
        var end = filter.IndexOf('\'', start);
        return end > start ? filter[start..end] : null;
    }
}
