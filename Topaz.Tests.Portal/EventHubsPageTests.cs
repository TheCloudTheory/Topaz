using Topaz.Portal.Components.Pages.EventHub;
using Topaz.Portal.Models.EventHubs;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class EventHubsPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void EventHubsPage_EmptyList_ShowsNoNamespacesMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListEventHubNamespaces(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListEventHubNamespacesResponse { Value = [] }));

        var cut = RenderComponent<EventHubs>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Event Hub namespaces found")));
    }
}

[TestFixture]
public class EventHubsPage_WithNamespaces_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void EventHubsPage_WithNamespaces_ShowsTable()
    {
        var subId = Guid.NewGuid();
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));
        _client.ListEventHubNamespaces(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListEventHubNamespacesResponse
            {
                Value =
                [
                    new EventHubNamespaceDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.EventHub/namespaces/my-namespace",
                        Name = "my-namespace",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        SkuName = "Standard"
                    }
                ]
            }));

        var cut = RenderComponent<EventHubs>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("my-namespace")), Is.True,
                "Expected the namespace name to appear in the table.");
        });
    }
}

[TestFixture]
public class EventHubsPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task EventHubsPage_CreatePanel_CreatesAndRefreshesNamespaceList()
    {
        var subId = Guid.NewGuid();
        const string namespaceName = "new-namespace";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListEventHubNamespaces(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ListEventHubNamespacesResponse { Value = [] }),
                Task.FromResult(new ListEventHubNamespacesResponse
                {
                    Value =
                    [
                        new EventHubNamespaceDto
                        {
                            Name = namespaceName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            SkuName = "Standard"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateEventHubNamespace(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<EventHubs>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Event Hub namespaces found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select subscription
        cut.Find("select").Change(subId.ToString("D"));

        // Wait for resource group dropdown
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group
        cut.FindAll("select")[1].Change("rg1");

        // Fill in namespace name
        cut.Find("input[placeholder='e.g. my-eventhub-namespace']").Change(namespaceName);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert new namespace appears in the list
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(namespaceName)), Is.True,
                "Expected the new namespace name to appear in the table.");
        });

        await _client.Received(1).CreateEventHubNamespace(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == namespaceName),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
