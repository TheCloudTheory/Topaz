using System.Text.Json;
using Topaz.Service.AppService.Models;
using Topaz.Service.Shared;
using Topaz.Service.Shared.Domain;
using Topaz.Shared;

namespace Topaz.Service.AppService;

internal sealed class AppServiceSiteResourceProvider(ITopazLogger logger)
    : ResourceProviderBase<AppServiceSiteService>(logger)
{
    public (SubscriptionIdentifier Sub, ResourceGroupIdentifier Rg, AppServiceSiteResource Site)? FindSiteByName(
        string siteName)
    {
        // Physical layout: .topaz/.subscription/{subId}/.resource-group/{rg}/.azure-web-sites/{name}/metadata.json
        var subscriptionsRoot = Path.Combine(BaseEmulatorPath, ".subscription");
        if (!Directory.Exists(subscriptionsRoot)) return null;

        foreach (var subDir in Directory.EnumerateDirectories(subscriptionsRoot))
        {
            if (!Guid.TryParse(Path.GetFileName(subDir), out var subGuid)) continue;
            var sub = SubscriptionIdentifier.From(subGuid.ToString());

            var rgRoot = Path.Combine(subDir, ".resource-group");
            if (!Directory.Exists(rgRoot)) continue;

            foreach (var rgDir in Directory.EnumerateDirectories(rgRoot))
            {
                var rg = ResourceGroupIdentifier.From(Path.GetFileName(rgDir));
                var metadataFile = Path.Combine(rgDir, ".azure-web-sites", siteName, "metadata.json");

                if (!File.Exists(metadataFile)) continue;

                var json = File.ReadAllText(metadataFile);
                var site = JsonSerializer.Deserialize<AppServiceSiteResource>(json, GlobalSettings.JsonOptions);
                if (site != null) return (sub, rg, site);
            }
        }

        return null;
    }

    public string GetDeploymentsDirectory(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName)
    {
        var instancePath = GetServiceInstancePath(sub, rg, siteName);
        return Path.Combine(instancePath, "deployments");
    }

    public void SaveDeploymentZip(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName, string id,
        Stream body)
    {
        var dir = GetDeploymentsDirectory(sub, rg, siteName);
        Directory.CreateDirectory(dir);
        var zipPath = Path.Combine(dir, $"{id}.zip");
        using var fs = File.Create(zipPath);
        body.CopyTo(fs);
    }

    public void SaveDeploymentRecord(SubscriptionIdentifier sub, ResourceGroupIdentifier rg, string siteName, string id,
        DeploymentRecord record)
    {
        var dir = Path.Combine(GetDeploymentsDirectory(sub, rg, siteName), id);
        Directory.CreateDirectory(dir);
        var metadataPath = Path.Combine(dir, "metadata.json");
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(record, GlobalSettings.JsonOptions));
    }

    public IReadOnlyList<DeploymentRecord> ListDeploymentRecords(SubscriptionIdentifier sub, ResourceGroupIdentifier rg,
        string siteName)
    {
        var deploymentsDir = GetDeploymentsDirectory(sub, rg, siteName);
        if (!Directory.Exists(deploymentsDir)) return [];

        var records = new List<DeploymentRecord>();
        foreach (var deploymentDir in Directory.EnumerateDirectories(deploymentsDir))
        {
            var metadataPath = Path.Combine(deploymentDir, "metadata.json");
            if (!File.Exists(metadataPath)) continue;
            var json = File.ReadAllText(metadataPath);
            var record = JsonSerializer.Deserialize<DeploymentRecord>(json, GlobalSettings.JsonOptions);
            if (record != null) records.Add(record);
        }

        return records;
    }
}

