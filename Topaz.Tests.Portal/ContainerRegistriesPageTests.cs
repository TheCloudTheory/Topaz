using Topaz.Portal.Components.Pages.ContainerRegistry;
using Topaz.Portal.Models.ContainerRegistry;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ContainerRegistriesPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ContainerRegistriesPage_EmptyList_ShowsNoRegistriesMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListContainerRegistries()
            .Returns(Task.FromResult(new ListContainerRegistriesResponse { Value = [] }));

        var cut = RenderComponent<ContainerRegistries>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No container registries found")));
    }
}

[TestFixture]
public class ContainerRegistriesPage_WithRegistries_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ContainerRegistriesPage_WithRegistries_ShowsTable()
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
        _client.ListContainerRegistries()
            .Returns(Task.FromResult(new ListContainerRegistriesResponse
            {
                Value =
                [
                    new ContainerRegistryDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.ContainerRegistry/registries/myregistry",
                        Name = "myregistry",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        SkuName = "Basic",
                        LoginServer = "myregistry.azurecr.io"
                    }
                ]
            }));

        var cut = RenderComponent<ContainerRegistries>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("myregistry")), Is.True,
                "Expected the registry name to appear in the table.");
        });
    }
}

[TestFixture]
public class ContainerRegistriesPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task ContainerRegistriesPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subId = Guid.NewGuid();
        const string registryName = "mynewregistry";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListContainerRegistries()
            .Returns(
                Task.FromResult(new ListContainerRegistriesResponse { Value = [] }),
                Task.FromResult(new ListContainerRegistriesResponse
                {
                    Value =
                    [
                        new ContainerRegistryDto
                        {
                            Name = registryName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            SkuName = "Basic",
                            LoginServer = $"{registryName}.azurecr.io"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateContainerRegistry(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<ContainerRegistries>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No container registries found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select subscription
        cut.Find("select").Change(subId.ToString("D"));

        // Wait for resource group dropdown to populate
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group
        var selects = cut.FindAll("select");
        selects[1].Change("rg1");

        // Fill in registry name
        cut.Find("input[placeholder='e.g. myregistry']").Change(registryName);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert new registry appears in the list
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(registryName)), Is.True,
                "Expected the new registry name to appear in the table.");
        });

        await _client.Received(1).CreateContainerRegistry(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == registryName),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
