using JetBrains.Annotations;
using Topaz.Service.ContainerRegistry.Models.Requests;

namespace Topaz.Service.ContainerRegistry.Models;

[UsedImplicitly]
internal sealed class ContainerRegistryResourceProperties
{
    public string? LoginServer { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public string ProvisioningState { get; set; } = "Succeeded";
    public bool AdminUserEnabled { get; set; }
    public string? AdminUsername { get; set; }
    public string? AdminPassword { get; set; }
    public string? AdminPassword2 { get; set; }
    public bool DataEndpointEnabled { get; set; }
    public string PublicNetworkAccess { get; set; } = "Enabled";
    public string ZoneRedundancy { get; set; } = "Disabled";
    public string NetworkRuleBypassOptions { get; set; } = "AzureServices";

    public static ContainerRegistryResourceProperties FromRequest(string registryName, CreateOrUpdateContainerRegistryRequest request)
    {
        return new ContainerRegistryResourceProperties
        {
            LoginServer = $"{registryName}.cr.topaz.local.dev",
            CreationDate = DateTimeOffset.UtcNow,
            ProvisioningState = "Succeeded",
            AdminUserEnabled = request.Properties?.AdminUserEnabled.GetValueOrDefault(false) ?? false,
            AdminUsername = (request.Properties?.AdminUserEnabled.GetValueOrDefault(false) ?? false) ? registryName : null,
            AdminPassword = (request.Properties?.AdminUserEnabled.GetValueOrDefault(false) ?? false) ? GenerateAdminPassword() : null,
            AdminPassword2 = (request.Properties?.AdminUserEnabled.GetValueOrDefault(false) ?? false) ? GenerateAdminPassword() : null,
            DataEndpointEnabled = request.Properties?.DataEndpointEnabled.GetValueOrDefault(false) ?? false,
            PublicNetworkAccess = request.Properties?.PublicNetworkAccess ?? "Enabled",
            ZoneRedundancy = request.Properties?.ZoneRedundancy ?? "Disabled",
            NetworkRuleBypassOptions = request.Properties?.NetworkRuleBypassOptions ?? "AzureServices"
        };
    }

    public static void UpdateFromRequest(ContainerRegistryResource resource, CreateOrUpdateContainerRegistryRequest request)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (request.Properties == null) return;

        if (request.Properties.AdminUserEnabled.HasValue)
        {
            resource.Properties.AdminUserEnabled = request.Properties.AdminUserEnabled.Value;
            if (request.Properties.AdminUserEnabled.Value && resource.Properties.AdminUsername == null)
            {
                resource.Properties.AdminUsername = resource.Name ?? string.Empty;
                resource.Properties.AdminPassword = GenerateAdminPassword();
                resource.Properties.AdminPassword2 = GenerateAdminPassword();
            }
            else if (!request.Properties.AdminUserEnabled.Value)
            {
                resource.Properties.AdminUsername = null;
                resource.Properties.AdminPassword = null;
                resource.Properties.AdminPassword2 = null;
            }
        }

        if (request.Properties.DataEndpointEnabled.HasValue)
            resource.Properties.DataEndpointEnabled = request.Properties.DataEndpointEnabled.Value;

        if (request.Properties.PublicNetworkAccess != null)
            resource.Properties.PublicNetworkAccess = request.Properties.PublicNetworkAccess;

        if (request.Properties.ZoneRedundancy != null)
            resource.Properties.ZoneRedundancy = request.Properties.ZoneRedundancy;

        if (request.Properties.NetworkRuleBypassOptions != null)
            resource.Properties.NetworkRuleBypassOptions = request.Properties.NetworkRuleBypassOptions;
    }

    internal static string GenerateAdminPassword() => Guid.NewGuid().ToString("N");

    public static RegistryUsage[] GetUsagesForSku(string? skuName)
    {
        var (storageLimit, webhookLimit) = skuName switch
        {
            "Premium" => (536870912000L, 500L),
            "Standard" => (107374182400L, 10L),
            _ => (10737418240L, 2L)
        };

        return
        [
            new RegistryUsage("Size", storageLimit, 0, "Bytes"),
            new RegistryUsage("Webhooks", webhookLimit, 0, "Count")
        ];
    }
}
