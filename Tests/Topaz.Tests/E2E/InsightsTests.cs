using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApplicationInsights;
using Azure.ResourceManager.ApplicationInsights.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class InsightsTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-1111-0000-0000-AC0300000000");

    private const string SubscriptionName = "sub-e2e-insights";
    private const string ResourceGroupName = "rg-e2e-insights";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static ApplicationInsightsComponentData MinimalComponentData() =>
        new(AzureLocation.WestEurope, "web")
        {
            ApplicationType = ApplicationInsightsApplicationType.Web,
        };

    private async Task<ResourceGroupResource> GetResourceGroup(ArmClient client)
    {
        var sub = await client.GetDefaultSubscriptionAsync();
        return (await sub.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    [Test]
    public async Task Insights_Create_ComponentIsAvailable()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string componentName = "e2e-insights-create";

        var result = await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, MinimalComponentData());

        var component = result.Value;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Data.Name, Is.EqualTo(componentName));
            Assert.That(component.Data.ResourceType, Is.EqualTo(new ResourceType("microsoft.insights/components")));
            Assert.That(component.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(component.Data.ProvisioningState, Is.EqualTo("Succeeded").IgnoreCase);
        }
    }

    [Test]
    public async Task Insights_Get_ReturnsComponent()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string componentName = "e2e-insights-get";

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, MinimalComponentData());

        var component = (await rg.GetApplicationInsightsComponents().GetAsync(componentName)).Value;

        Assert.That(component.Data.Name, Is.EqualTo(componentName));
    }

    [Test]
    public async Task Insights_Get_ReturnsInstrumentationKeyAndConnectionString()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string componentName = "e2e-insights-keys";

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, MinimalComponentData());

        var component = (await rg.GetApplicationInsightsComponents().GetAsync(componentName)).Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Data.InstrumentationKey, Is.Not.Null.And.Not.Empty);
            Assert.That(component.Data.ConnectionString, Does.Contain("InstrumentationKey="));
        }
    }

    [Test]
    public async Task Insights_Delete_ComponentNotAvailableAfterDelete()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string componentName = "e2e-insights-delete";

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, MinimalComponentData());

        var component = (await rg.GetApplicationInsightsComponents().GetAsync(componentName)).Value;
        await component.DeleteAsync(WaitUntil.Completed);

        Assert.That(
            async () => await rg.GetApplicationInsightsComponents().GetAsync(componentName),
            Throws.InstanceOf<RequestFailedException>());
    }

    [Test]
    public async Task Insights_List_ByResourceGroup_AllComponentsAppear()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-insights-list-a", MinimalComponentData());
        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-insights-list-b", MinimalComponentData());

        var names = new List<string>();
        await foreach (var component in rg.GetApplicationInsightsComponents().GetAllAsync())
            names.Add(component.Data.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain("e2e-insights-list-a"));
            Assert.That(names, Does.Contain("e2e-insights-list-b"));
        }
    }

    [Test]
    public async Task Insights_List_BySubscription_AllComponentsAppear()
    {
        var client = CreateClient();
        var sub = await client.GetDefaultSubscriptionAsync();
        var rg = await GetResourceGroup(client);

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-insights-subslist-a", MinimalComponentData());
        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-insights-subslist-b", MinimalComponentData());

        var names = new List<string>();
        await foreach (var component in sub.GetApplicationInsightsComponentsAsync())
            names.Add(component.Data.Name);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain("e2e-insights-subslist-a"));
            Assert.That(names, Does.Contain("e2e-insights-subslist-b"));
        }
    }

    [Test]
    public async Task Insights_Update_TagsAreUpdated()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string componentName = "e2e-insights-update";

        await rg.GetApplicationInsightsComponents()
            .CreateOrUpdateAsync(WaitUntil.Completed, componentName, MinimalComponentData());

        var component = (await rg.GetApplicationInsightsComponents().GetAsync(componentName)).Value;

        var patch = new WebTestComponentTag
        {
            Tags =
            {
                ["env"] = "test"
            }
        };
        var updated = (await component.UpdateAsync(patch)).Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Data.Tags.ContainsKey("env"), Is.True);
            Assert.That(updated.Data.Tags["env"], Is.EqualTo("test"));
        }
    }

    [Test]
    public async Task Insights_ArmDeployment_CreatesComponentWithLogAnalyticsWorkspace()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "sub-insights-arm-deploy");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-insights-arm-deploy",
            new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "deploy-insights",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-insights.json"))
            }));

        var component = (await rg.Value.GetApplicationInsightsComponents().GetAsync("topaz-insights-deploy01")).Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Data.Name, Is.EqualTo("topaz-insights-deploy01"));
            Assert.That(component.Data.WorkspaceResourceId.ToString(), Is.Not.Null.And.Not.Empty);
            Assert.That(component.Data.InstrumentationKey, Is.Not.Null.And.Not.Empty);
            Assert.That(component.Data.ConnectionString, Does.Contain("InstrumentationKey="));
        }
    }
}
