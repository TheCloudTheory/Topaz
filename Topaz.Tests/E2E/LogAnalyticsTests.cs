using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.OperationalInsights;
using Azure.ResourceManager.OperationalInsights.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E;

public class LogAnalyticsTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A9C8B7D6-1111-0000-0000-AC0300000000");

    private const string SubscriptionName = "sub-e2e-loganalytics";
    private const string ResourceGroupName = "rg-e2e-loganalytics";

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

    private static OperationalInsightsWorkspaceData MinimalWorkspaceData() =>
        new(AzureLocation.WestEurope)
        {
            Sku = new OperationalInsightsWorkspaceSku(OperationalInsightsWorkspaceSkuName.PerGB2018),
            RetentionInDays = 30
        };

    private async Task<ResourceGroupResource> GetResourceGroup(ArmClient client)
    {
        var sub = await client.GetDefaultSubscriptionAsync();
        return (await sub.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    [Test]
    public async Task LogAnalytics_Create_WorkspaceIsAvailable()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-create";

        var result = await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(workspace.Data.Name, Is.EqualTo(workspaceName));
            Assert.That(workspace.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.OperationalInsights/workspaces")));
            Assert.That(workspace.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(workspace.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded").IgnoreCase);
        });
    }

    [Test]
    public async Task LogAnalytics_Create_WorkspaceIdAndCustomerIdArePopulated()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-ids";

        var result = await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = result.Value;
        Assert.Multiple(() =>
        {
            Assert.That(workspace.Data.CustomerId, Is.Not.Null);
        });
    }

    [Test]
    public async Task LogAnalytics_Create_WorkspaceIdIsStableAcrossUpdates()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-stable-id";

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var before = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;
        var idBefore = before.Data.CustomerId;

        // PUT again (idempotent update)
        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var after = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;

        Assert.That(after.Data.CustomerId, Is.EqualTo(idBefore));
    }

    [Test]
    public async Task LogAnalytics_Get_ReturnsWorkspace()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-get";

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;

        Assert.That(workspace.Data.Name, Is.EqualTo(workspaceName));
    }

    [Test]
    public async Task LogAnalytics_Delete_WorkspaceIsNotAvailableAfterDelete()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-delete";

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;
        await workspace.DeleteAsync(WaitUntil.Completed);

        Assert.That(
            async () => await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName),
            Throws.InstanceOf<RequestFailedException>());
    }

    [Test]
    public async Task LogAnalytics_List_AllWorkspacesAppear()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-la-list-a", MinimalWorkspaceData());
        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, "e2e-la-list-b", MinimalWorkspaceData());

        var names = new List<string>();
        await foreach (var ws in rg.GetOperationalInsightsWorkspaces().GetAllAsync())
            names.Add(ws.Data.Name);

        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("e2e-la-list-a"));
            Assert.That(names, Does.Contain("e2e-la-list-b"));
        });
    }

    [Test]
    public async Task LogAnalytics_Update_RetentionInDaysIsChanged()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-update-retention";

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;
        var patch = new OperationalInsightsWorkspacePatch { RetentionInDays = 60 };
        var updated = (await workspace.UpdateAsync(patch)).Value;

        Assert.That(updated.Data.RetentionInDays, Is.EqualTo(60));
    }

    [Test]
    public async Task LogAnalytics_Update_TagsAreChanged()
    {
        var client = CreateClient();
        var rg = await GetResourceGroup(client);
        const string workspaceName = "e2e-la-update-tags";

        await rg.GetOperationalInsightsWorkspaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, workspaceName, MinimalWorkspaceData());

        var workspace = (await rg.GetOperationalInsightsWorkspaces().GetAsync(workspaceName)).Value;
        var patch = new OperationalInsightsWorkspacePatch();
        patch.Tags["env"] = "test";
        var updated = (await workspace.UpdateAsync(patch)).Value;

        Assert.That(updated.Data.Tags, Does.ContainKey("env").WithValue("test"));
    }
}
