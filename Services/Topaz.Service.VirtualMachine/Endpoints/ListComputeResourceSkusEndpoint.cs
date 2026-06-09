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
    // Common VM SKUs with PremiumIO support flag — sufficient for ACE CapabilitiesCache.
    private static readonly (string Name, string Tier, bool PremiumIo)[] KnownSkus =
    [
        ("Standard_A1_v2",   "Standard", false),
        ("Standard_A2_v2",   "Standard", false),
        ("Standard_B2s",     "Standard", false),
        ("Standard_B2ms",    "Standard", false),
        ("Standard_B4ms",    "Standard", false),
        ("Standard_D2s_v3",  "Standard", true),
        ("Standard_D4s_v3",  "Standard", true),
        ("Standard_D8s_v3",  "Standard", true),
        ("Standard_D2_v3",   "Standard", false),
        ("Standard_D4_v3",   "Standard", false),
        ("Standard_E2s_v3",  "Standard", true),
        ("Standard_E4s_v3",  "Standard", true),
        ("Standard_F2s_v2",  "Standard", true),
        ("Standard_F4s_v2",  "Standard", true),
        ("Standard_DS1_v2",  "Standard", true),
        ("Standard_DS2_v2",  "Standard", true),
        ("Standard_DS3_v2",  "Standard", true),
    ];

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

            var skus = KnownSkus
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
