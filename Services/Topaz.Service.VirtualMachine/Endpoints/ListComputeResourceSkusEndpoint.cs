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
    private static readonly (string Name, string Tier, bool PremiumIo)[] KnownSkus =
    [
        // A-series v2 — no Premium Storage
        ("Standard_A1_v2",    "Standard", false),
        ("Standard_A2_v2",    "Standard", false),
        ("Standard_A4_v2",    "Standard", false),
        ("Standard_A8_v2",    "Standard", false),
        ("Standard_A2m_v2",   "Standard", false),
        ("Standard_A4m_v2",   "Standard", false),
        ("Standard_A8m_v2",   "Standard", false),

        // B-series burstable
        ("Standard_B1s",      "Standard", true),
        ("Standard_B1ms",     "Standard", true),
        ("Standard_B2s",      "Standard", true),
        ("Standard_B2ms",     "Standard", true),
        ("Standard_B4ms",     "Standard", true),
        ("Standard_B8ms",     "Standard", true),
        ("Standard_B12ms",    "Standard", true),
        ("Standard_B16ms",    "Standard", true),
        ("Standard_B20ms",    "Standard", true),

        // D-series v2 (DS = Premium)
        ("Standard_DS1_v2",   "Standard", true),
        ("Standard_DS2_v2",   "Standard", true),
        ("Standard_DS3_v2",   "Standard", true),
        ("Standard_DS4_v2",   "Standard", true),
        ("Standard_DS5_v2",   "Standard", true),
        ("Standard_D1_v2",    "Standard", false),
        ("Standard_D2_v2",    "Standard", false),
        ("Standard_D3_v2",    "Standard", false),
        ("Standard_D4_v2",    "Standard", false),
        ("Standard_D5_v2",    "Standard", false),

        // D-series v3
        ("Standard_D2s_v3",   "Standard", true),
        ("Standard_D4s_v3",   "Standard", true),
        ("Standard_D8s_v3",   "Standard", true),
        ("Standard_D16s_v3",  "Standard", true),
        ("Standard_D32s_v3",  "Standard", true),
        ("Standard_D48s_v3",  "Standard", true),
        ("Standard_D64s_v3",  "Standard", true),
        ("Standard_D2_v3",    "Standard", false),
        ("Standard_D4_v3",    "Standard", false),
        ("Standard_D8_v3",    "Standard", false),
        ("Standard_D16_v3",   "Standard", false),
        ("Standard_D32_v3",   "Standard", false),
        ("Standard_D48_v3",   "Standard", false),
        ("Standard_D64_v3",   "Standard", false),

        // D-series v4
        ("Standard_D2s_v4",   "Standard", true),
        ("Standard_D4s_v4",   "Standard", true),
        ("Standard_D8s_v4",   "Standard", true),
        ("Standard_D16s_v4",  "Standard", true),
        ("Standard_D32s_v4",  "Standard", true),
        ("Standard_D48s_v4",  "Standard", true),
        ("Standard_D64s_v4",  "Standard", true),
        ("Standard_D2_v4",    "Standard", false),
        ("Standard_D4_v4",    "Standard", false),
        ("Standard_D8_v4",    "Standard", false),
        ("Standard_D16_v4",   "Standard", false),
        ("Standard_D32_v4",   "Standard", false),
        ("Standard_D48_v4",   "Standard", false),
        ("Standard_D64_v4",   "Standard", false),

        // D-series v5
        ("Standard_D2s_v5",   "Standard", true),
        ("Standard_D4s_v5",   "Standard", true),
        ("Standard_D8s_v5",   "Standard", true),
        ("Standard_D16s_v5",  "Standard", true),
        ("Standard_D32s_v5",  "Standard", true),
        ("Standard_D48s_v5",  "Standard", true),
        ("Standard_D64s_v5",  "Standard", true),
        ("Standard_D96s_v5",  "Standard", true),
        ("Standard_D2_v5",    "Standard", false),
        ("Standard_D4_v5",    "Standard", false),
        ("Standard_D8_v5",    "Standard", false),
        ("Standard_D16_v5",   "Standard", false),
        ("Standard_D32_v5",   "Standard", false),
        ("Standard_D48_v5",   "Standard", false),
        ("Standard_D64_v5",   "Standard", false),
        ("Standard_D96_v5",   "Standard", false),

        // E-series v3
        ("Standard_E2s_v3",   "Standard", true),
        ("Standard_E4s_v3",   "Standard", true),
        ("Standard_E8s_v3",   "Standard", true),
        ("Standard_E16s_v3",  "Standard", true),
        ("Standard_E20s_v3",  "Standard", true),
        ("Standard_E32s_v3",  "Standard", true),
        ("Standard_E48s_v3",  "Standard", true),
        ("Standard_E64s_v3",  "Standard", true),
        ("Standard_E2_v3",    "Standard", false),
        ("Standard_E4_v3",    "Standard", false),
        ("Standard_E8_v3",    "Standard", false),
        ("Standard_E16_v3",   "Standard", false),
        ("Standard_E20_v3",   "Standard", false),
        ("Standard_E32_v3",   "Standard", false),
        ("Standard_E48_v3",   "Standard", false),
        ("Standard_E64_v3",   "Standard", false),

        // E-series v4
        ("Standard_E2s_v4",   "Standard", true),
        ("Standard_E4s_v4",   "Standard", true),
        ("Standard_E8s_v4",   "Standard", true),
        ("Standard_E16s_v4",  "Standard", true),
        ("Standard_E20s_v4",  "Standard", true),
        ("Standard_E32s_v4",  "Standard", true),
        ("Standard_E48s_v4",  "Standard", true),
        ("Standard_E64s_v4",  "Standard", true),
        ("Standard_E2_v4",    "Standard", false),
        ("Standard_E4_v4",    "Standard", false),
        ("Standard_E8_v4",    "Standard", false),
        ("Standard_E16_v4",   "Standard", false),
        ("Standard_E20_v4",   "Standard", false),
        ("Standard_E32_v4",   "Standard", false),
        ("Standard_E48_v4",   "Standard", false),
        ("Standard_E64_v4",   "Standard", false),

        // E-series v5
        ("Standard_E2s_v5",   "Standard", true),
        ("Standard_E4s_v5",   "Standard", true),
        ("Standard_E8s_v5",   "Standard", true),
        ("Standard_E16s_v5",  "Standard", true),
        ("Standard_E20s_v5",  "Standard", true),
        ("Standard_E32s_v5",  "Standard", true),
        ("Standard_E48s_v5",  "Standard", true),
        ("Standard_E64s_v5",  "Standard", true),
        ("Standard_E96s_v5",  "Standard", true),
        ("Standard_E2_v5",    "Standard", false),
        ("Standard_E4_v5",    "Standard", false),
        ("Standard_E8_v5",    "Standard", false),
        ("Standard_E16_v5",   "Standard", false),
        ("Standard_E20_v5",   "Standard", false),
        ("Standard_E32_v5",   "Standard", false),
        ("Standard_E48_v5",   "Standard", false),
        ("Standard_E64_v5",   "Standard", false),
        ("Standard_E96_v5",   "Standard", false),

        // F-series v2 (all have 's')
        ("Standard_F2s_v2",   "Standard", true),
        ("Standard_F4s_v2",   "Standard", true),
        ("Standard_F8s_v2",   "Standard", true),
        ("Standard_F16s_v2",  "Standard", true),
        ("Standard_F32s_v2",  "Standard", true),
        ("Standard_F48s_v2",  "Standard", true),
        ("Standard_F72s_v2",  "Standard", true),

        // G-series
        ("Standard_G1",       "Standard", false),
        ("Standard_G2",       "Standard", false),
        ("Standard_G3",       "Standard", false),
        ("Standard_G4",       "Standard", false),
        ("Standard_G5",       "Standard", false),
        ("Standard_GS1",      "Standard", true),
        ("Standard_GS2",      "Standard", true),
        ("Standard_GS3",      "Standard", true),
        ("Standard_GS4",      "Standard", true),
        ("Standard_GS5",      "Standard", true),

        // L-series v2
        ("Standard_L8s_v2",   "Standard", true),
        ("Standard_L16s_v2",  "Standard", true),
        ("Standard_L32s_v2",  "Standard", true),
        ("Standard_L48s_v2",  "Standard", true),
        ("Standard_L64s_v2",  "Standard", true),
        ("Standard_L80s_v2",  "Standard", true),

        // L-series v3
        ("Standard_L8s_v3",   "Standard", true),
        ("Standard_L16s_v3",  "Standard", true),
        ("Standard_L32s_v3",  "Standard", true),
        ("Standard_L48s_v3",  "Standard", true),
        ("Standard_L64s_v3",  "Standard", true),
        ("Standard_L80s_v3",  "Standard", true),

        // M-series
        ("Standard_M8ms",     "Standard", true),
        ("Standard_M16ms",    "Standard", true),
        ("Standard_M32ms",    "Standard", true),
        ("Standard_M32ls",    "Standard", true),
        ("Standard_M64ms",    "Standard", true),
        ("Standard_M64ls",    "Standard", true),
        ("Standard_M64s",     "Standard", true),
        ("Standard_M64",      "Standard", false),
        ("Standard_M128ms",   "Standard", true),
        ("Standard_M128s",    "Standard", true),
        ("Standard_M128",     "Standard", false),

        // N-series GPU — only s-variants support Premium
        ("Standard_NC6",      "Standard", false),
        ("Standard_NC12",     "Standard", false),
        ("Standard_NC24",     "Standard", false),
        ("Standard_NC6s_v3",  "Standard", true),
        ("Standard_NC12s_v3", "Standard", true),
        ("Standard_NC24s_v3", "Standard", true),
        ("Standard_NC4as_T4_v3",  "Standard", true),
        ("Standard_NC8as_T4_v3",  "Standard", true),
        ("Standard_NC16as_T4_v3", "Standard", true),
        ("Standard_NC64as_T4_v3", "Standard", true),
        ("Standard_NV6",      "Standard", false),
        ("Standard_NV12",     "Standard", false),
        ("Standard_NV24",     "Standard", false),
        ("Standard_NV12s_v3", "Standard", true),
        ("Standard_NV24s_v3", "Standard", true),
        ("Standard_NV48s_v3", "Standard", true),
        ("Standard_ND6s",     "Standard", true),
        ("Standard_ND12s",    "Standard", true),
        ("Standard_ND24s",    "Standard", true),
        ("Standard_ND40rs_v2", "Standard", true),
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
