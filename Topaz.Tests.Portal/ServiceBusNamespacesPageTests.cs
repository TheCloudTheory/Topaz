using Topaz.Portal.Components.Pages.ServiceBus;
using Topaz.Portal.Models.ServiceBus;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ServiceBusNamespacesPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ServiceBusNamespacesPage_EmptyList_ShowsNoNamespacesMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListServiceBusNamespaces(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListServiceBusNamespacesResponse { Value = [] }));

        var cut = Render<ServiceBusNamespaces>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Service Bus namespaces found")));
    }
}

[TestFixture]
public class ServiceBusNamespacesPage_WithNamespaces_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ServiceBusNamespacesPage_WithNamespaces_ShowsTable()
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
        _client.ListServiceBusNamespaces(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListServiceBusNamespacesResponse
            {
                Value =
                [
                    new ServiceBusNamespaceDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.ServiceBus/namespaces/my-sb-namespace",
                        Name = "my-sb-namespace",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        SkuName = "Standard"
                    }
                ]
            }));

        var cut = Render<ServiceBusNamespaces>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("my-sb-namespace")), Is.True,
                "Expected the namespace name to appear in the table.");
        });
    }
}

[TestFixture]
public class ServiceBusNamespacesPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task ServiceBusNamespacesPage_CreatePanel_CreatesAndRefreshesNamespaceList()
    {
        var subId = Guid.NewGuid();
        const string namespaceName = "new-sb-namespace";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListServiceBusNamespaces(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ListServiceBusNamespacesResponse { Value = [] }),
                Task.FromResult(new ListServiceBusNamespacesResponse
                {
                    Value =
                    [
                        new ServiceBusNamespaceDto
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

        _client.CreateServiceBusNamespace(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<ServiceBusNamespaces>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Service Bus namespaces found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select subscription
        cut.Find("select").Change(subId.ToString("D"));

        // Wait for resource group dropdown
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group
        cut.FindAll("select")[1].Change("rg1");

        // Fill in namespace name
        cut.Find("input[placeholder='e.g. my-servicebus-namespace']").Change(namespaceName);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert new namespace appears in the list
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(namespaceName)), Is.True,
                "Expected the new namespace name to appear in the table.");
        });

        await _client.Received(1).CreateServiceBusNamespace(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == namespaceName),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
