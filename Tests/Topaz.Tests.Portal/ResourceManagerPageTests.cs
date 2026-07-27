using Topaz.Portal.Components.Pages.ResourceManager;
using Topaz.Portal.Models.KeyVaults;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.Storage;

namespace Topaz.Tests.Portal;

[TestFixture]
public class AllResourcesPage_ShowsAggregatedResources_WhenLoaded : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListStorageAccounts().Returns(Task.FromResult(new ListStorageAccountsResponse
        {
            Value =
            [
                new StorageAccountDto { Name = "mystorage", Location = "westeurope", ResourceGroupName = "rg-dev", SubscriptionName = "Dev", SubscriptionId = Guid.NewGuid().ToString("D") }
            ]
        }));
        client.ListKeyVaults().Returns(Task.FromResult(new ListKeyVaultsResponse
        {
            Value =
            [
                new KeyVaultDto { Name = "myvault", Location = "eastus", ResourceGroupName = "rg-prod", SubscriptionName = "Prod", SubscriptionId = Guid.NewGuid().ToString("D") }
            ]
        }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = Render<AllResourcesPage>();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(cut.Markup, Does.Contain("mystorage"));
            Assert.That(cut.Markup, Does.Contain("myvault"));
            Assert.That(cut.Markup, Does.Contain("Microsoft.Storage/storageAccounts"));
            Assert.That(cut.Markup, Does.Contain("Microsoft.KeyVault/vaults"));
        });
    }
}

[TestFixture]
public class AllResourcesPage_ShowsEmptyMessage_WhenNoResources : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListStorageAccounts().Returns(Task.FromResult(new ListStorageAccountsResponse { Value = [] }));
        client.ListKeyVaults().Returns(Task.FromResult(new ListKeyVaultsResponse { Value = [] }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = Render<AllResourcesPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No resources found")));
    }
}

[TestFixture]
public class AllResourcesPage_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListStorageAccounts().Returns<ListStorageAccountsResponse>(_ => throw new InvalidOperationException("arm down"));
        client.ListKeyVaults().Returns(Task.FromResult(new ListKeyVaultsResponse { Value = [] }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = Render<AllResourcesPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("arm down")));
    }
}

[TestFixture]
public class ManagementGroupsPage_ShowsEmptyState_WhenNoGroups : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(
                new Topaz.Portal.Models.ManagementGroups.GetManagementGroupEntitiesResponse { Value = [] }));

        var cut = Render<ManagementGroupsPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No management groups found")));
    }
}

[TestFixture]
public class ResourceManagerDeploymentsPage_ShowsHint_OnLoad : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse
        {
            Value = [new SubscriptionDto { SubscriptionId = Guid.NewGuid().ToString("D"), DisplayName = "Dev" }]
        }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = Render<ResourceManagerDeploymentsPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("Select a subscription and resource group")));
    }
}

[TestFixture]
public class ResourceManagerDeploymentsPage_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListSubscriptions().Returns<ListSubscriptionsResponse>(_ => throw new InvalidOperationException("network failure"));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = Render<ResourceManagerDeploymentsPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("network failure")));
    }
}
