using System.Text.Json;
using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class AppServiceSiteConfigTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("E1F2A3B4-0000-0000-0000-CC0500000007");
    private const string SubscriptionName = "sub-test-appservice-config";
    private const string ResourceGroupName = "rg-test-appservice-config";
    private const string PlanName = "plan-test-config";
    private const string SiteName = "site-test-config";

    private string SiteMetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription", SubscriptionId.ToString(),
        ".resource-group", ResourceGroupName, ".azure-web-sites", SiteName, "metadata.json");

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync([
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "appservice", "plan", "create",
            "--name", PlanName,
            "-g", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "appservice", "site", "create",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task AppServiceSiteConfig_WhenConfigWebIsRead_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appservice", "site", "config", "get-web",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppServiceSiteConfig_WhenConfigWebIsUpdated_ItShouldPersistChanges()
    {
        var code = await Program.RunAsync([
            "appservice", "site", "config", "update-web",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--always-on", "true"
        ]);

        Assert.That(code, Is.EqualTo(0));

        var json = await File.ReadAllTextAsync(SiteMetadataPath);
        using var doc = JsonDocument.Parse(json);
        var alwaysOn = doc.RootElement
            .GetProperty("properties")
            .GetProperty("siteConfig")
            .GetProperty("alwaysOn")
            .GetBoolean();
        Assert.That(alwaysOn, Is.True);
    }

    [Test]
    public async Task AppServiceSiteConfig_WhenAppSettingsAreSet_CommandShouldSucceed()
    {
        var code = await Program.RunAsync([
            "appservice", "site", "config", "set-appsettings",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--settings", "MYKEY=MYVALUE"
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task AppServiceSiteConfig_WhenAppSettingsAreSet_TheyShouldBePersisted()
    {
        await Program.RunAsync([
            "appservice", "site", "config", "set-appsettings",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--settings", "PERSISTKEY=PERSISTVALUE"
        ]);

        var json = await File.ReadAllTextAsync(SiteMetadataPath);
        using var doc = JsonDocument.Parse(json);
        var appSettings = doc.RootElement
            .GetProperty("properties")
            .GetProperty("siteConfig")
            .GetProperty("appSettings")
            .EnumerateArray()
            .ToList();

        Assert.That(appSettings.Any(s =>
            s.GetProperty("name").GetString() == "PERSISTKEY" &&
            s.GetProperty("value").GetString() == "PERSISTVALUE"), Is.True);
    }

    [Test]
    public async Task AppServiceSiteConfig_WhenAppSettingsAreListed_CommandShouldSucceed()
    {
        await Program.RunAsync([
            "appservice", "site", "config", "set-appsettings",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--settings", "LISTKEY=LISTVALUE"
        ]);

        var code = await Program.RunAsync([
            "appservice", "site", "config", "list-appsettings",
            "--name", SiteName,
            "-g", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
