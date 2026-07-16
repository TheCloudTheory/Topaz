using Topaz.Portal.Components.Shared;
using Topaz.Portal.Models.CosmosDb;
using Topaz.Portal.Models.EventHubs;
using Topaz.Portal.Models.KeyVaults;
using Topaz.Portal.Models.ManagedIdentities;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.ResourceManager;
using Topaz.Portal.Models.ServiceBus;
using Topaz.Portal.Models.Storage;
using Topaz.Portal.Models.Subscriptions;
using Topaz.Portal.Models.VirtualMachines;
using Topaz.Portal.Models.VirtualNetworks;

namespace Topaz.Tests.Portal;

// Each test class contains a single test so each gets a fresh BunitTestContext (new instance per class).
// Splitting tests across classes is the safe bUnit + NUnit pattern when multiple tests share a widget.

[TestFixture]
public class SubscriptionsWidget_ShowsCount_WhenSubscriptionsLoaded : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse
        {
            Value =
            [
                new SubscriptionDto { Id = "/subscriptions/aaa", SubscriptionId = "aaa", DisplayName = "Dev" },
                new SubscriptionDto { Id = "/subscriptions/bbb", SubscriptionId = "bbb", DisplayName = "Prod" }
            ]
        }));

        var cut = RenderComponent<SubscriptionsWidget>();

        cut.WaitForAssertion(() =>
        {
            var badge = cut.Find(".badge");
            Assert.That(badge.TextContent.Trim(), Is.EqualTo("2"));
        });
    }
}

[TestFixture]
public class SubscriptionsWidget_ShowsEmptyMessage_WhenNoSubscriptions : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);
        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));

        var cut = RenderComponent<SubscriptionsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No subscriptions found")));
    }
}

[TestFixture]
public class SubscriptionsWidget_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);
        client.ListSubscriptions().Returns<ListSubscriptionsResponse>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<SubscriptionsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("boom")));
    }
}

[TestFixture]
public class ResourceGroupsWidget_ShowsCount_WhenResourceGroupsLoaded : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse
        {
            Value =
            [
                new ResourceGroupDto { Id = "/rg/rg-dev", Name = "rg-dev", Location = "westeurope" },
                new ResourceGroupDto { Id = "/rg/rg-prod", Name = "rg-prod", Location = "eastus" },
                new ResourceGroupDto { Id = "/rg/rg-test", Name = "rg-test", Location = "northeurope" }
            ]
        }));

        var cut = RenderComponent<ResourceGroupsWidget>();

        cut.WaitForAssertion(() =>
        {
            var badge = cut.Find(".badge");
            Assert.That(badge.TextContent.Trim(), Is.EqualTo("3"));
        });
    }
}

[TestFixture]
public class ResourceGroupsWidget_ShowsEmptyMessage_WhenNoResourceGroups : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = RenderComponent<ResourceGroupsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No resource groups found")));
    }
}

[TestFixture]
public class ResourceGroupsWidget_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);
        client.ListResourceGroups().Returns<ListResourceGroupsResponse>(_ => throw new HttpRequestException("network error"));

        var cut = RenderComponent<ResourceGroupsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("network error")));
    }
}

[TestFixture]
public class ResourceSummaryWidget_ShowsStorageAndKeyVaultCounts : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListStorageAccounts().Returns(Task.FromResult(new ListStorageAccountsResponse
        {
            Value = [new StorageAccountDto { Name = "sa1" }, new StorageAccountDto { Name = "sa2" }]
        }));
        client.ListKeyVaults().Returns(Task.FromResult(new ListKeyVaultsResponse
        {
            Value = [new KeyVaultDto { Name = "kv1" }]
        }));
        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));
        client.ListManagedIdentities().Returns(Task.FromResult(new ListManagedIdentitiesResponse { Value = [] }));
        client.ListEventHubNamespaces().Returns(Task.FromResult(new ListEventHubNamespacesResponse { Value = [] }));
        client.ListServiceBusNamespaces().Returns(Task.FromResult(new ListServiceBusNamespacesResponse { Value = [] }));
        client.ListVirtualMachines().Returns(Task.FromResult(new ListVirtualMachinesResponse { Value = [] }));
        client.ListVirtualNetworks().Returns(Task.FromResult(new ListVirtualNetworksResponse { Value = [] }));
        client.ListCosmosDbAccounts().Returns(Task.FromResult(new ListCosmosDbAccountsResponse { Value = [] }));

        var cut = RenderComponent<ResourceSummaryWidget>();

        cut.WaitForAssertion(() =>
        {
            var counts = cut.FindAll(".display-6.fw-bold");
            Assert.That(counts.Any(c => c.TextContent.Trim() == "2"), Is.True, "Expected storage count 2");
            Assert.That(counts.Any(c => c.TextContent.Trim() == "1"), Is.True, "Expected key vault count 1");
        });
    }
}

[TestFixture]
public class ResourceSummaryWidget_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListStorageAccounts().Returns<ListStorageAccountsResponse>(_ => throw new InvalidOperationException("storage down"));
        client.ListKeyVaults().Returns(Task.FromResult(new ListKeyVaultsResponse { Value = [] }));
        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));
        client.ListManagedIdentities().Returns(Task.FromResult(new ListManagedIdentitiesResponse { Value = [] }));
        client.ListEventHubNamespaces().Returns(Task.FromResult(new ListEventHubNamespacesResponse { Value = [] }));
        client.ListServiceBusNamespaces().Returns(Task.FromResult(new ListServiceBusNamespacesResponse { Value = [] }));
        client.ListVirtualMachines().Returns(Task.FromResult(new ListVirtualMachinesResponse { Value = [] }));
        client.ListVirtualNetworks().Returns(Task.FromResult(new ListVirtualNetworksResponse { Value = [] }));
        client.ListCosmosDbAccounts().Returns(Task.FromResult(new ListCosmosDbAccountsResponse { Value = [] }));

        var cut = RenderComponent<ResourceSummaryWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("storage down")));
    }
}

// ── DeploymentsWidget ────────────────────────────────────────────────────────

[TestFixture]
public class DeploymentsWidget_ShowsDeployments_SortedByTimestamp : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        var subId = Guid.NewGuid();
        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse
        {
            Value = [new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }]
        }));

        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse
        {
            Value = [new ResourceGroupDto { Name = "rg-dev", SubscriptionId = subId.ToString("D"), Location = "westeurope" }]
        }));

        var older = DateTimeOffset.UtcNow.AddHours(-2);
        var newer = DateTimeOffset.UtcNow.AddMinutes(-5);

        client.ListDeployments(subId, "rg-dev", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListDeploymentsResponse
            {
                Value =
                [
                    new DeploymentDto { Name = "deploy-old", Properties = new DeploymentPropertiesDto { ProvisioningState = "Succeeded", Timestamp = older } },
                    new DeploymentDto { Name = "deploy-new", Properties = new DeploymentPropertiesDto { ProvisioningState = "Running",   Timestamp = newer  } }
                ]
            }));

        var cut = RenderComponent<DeploymentsWidget>();

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            Assert.That(rows.Count, Is.EqualTo(2));
            // First row should be the newest deployment
            Assert.That(rows[0].TextContent, Does.Contain("deploy-new"));
            Assert.That(rows[1].TextContent, Does.Contain("deploy-old"));
        });
    }
}

[TestFixture]
public class DeploymentsWidget_ShowsEmptyMessage_WhenNoDeployments : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListSubscriptions().Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = RenderComponent<DeploymentsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No deployments found")));
    }
}

[TestFixture]
public class DeploymentsWidget_ShowsError_WhenLoadFails : BunitTestContext
{
    [Test]
    public void Test()
    {
        var client = Substitute.For<ITopazClient>();
        Services.AddSingleton(client);

        client.ListSubscriptions().Returns<ListSubscriptionsResponse>(_ => throw new InvalidOperationException("arm unavailable"));
        client.ListResourceGroups().Returns(Task.FromResult(new ListResourceGroupsResponse { Value = [] }));

        var cut = RenderComponent<DeploymentsWidget>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("arm unavailable")));
    }
}
