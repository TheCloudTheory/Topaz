using Topaz.Portal.Components.Pages.CosmosDb;
using Topaz.Portal.Models.CosmosDb;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class CosmosDbAccountsPageEmptyListTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void CosmosDbAccountsPage_EmptyList_ShowsNoAccountsMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListCosmosDbAccounts(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListCosmosDbAccountsResponse { Value = [] }));

        var cut = Render<CosmosDbAccounts>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Cosmos DB accounts found")));
    }
}

[TestFixture]
public class CosmosDbAccountsPageWithAccountsTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void CosmosDbAccountsPage_WithAccounts_ShowsTable()
    {
        var subscriptionId = Guid.NewGuid();

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subscriptionId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListCosmosDbAccounts(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListCosmosDbAccountsResponse
            {
                Value =
                [
                    new CosmosDbAccountDto
                    {
                        Id = $"/subscriptions/{subscriptionId:D}/resourceGroups/rg1/providers/Microsoft.DocumentDB/databaseAccounts/cosmos-one",
                        Name = "cosmos-one",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subscriptionId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        Kind = "GlobalDocumentDB",
                        ProvisioningState = "Succeeded"
                    }
                ]
            }));

        var cut = Render<CosmosDbAccounts>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("cosmos-one")), Is.True,
                "Expected the Cosmos DB account name to appear in the table.");
        });
    }
}

[TestFixture]
public class CosmosDbAccountsPageCreateTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task CosmosDbAccountsPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subscriptionId = Guid.NewGuid();
        const string accountName = "cosmos-new-account";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subscriptionId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListCosmosDbAccounts(Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ListCosmosDbAccountsResponse { Value = [] }),
                Task.FromResult(new ListCosmosDbAccountsResponse
                {
                    Value =
                    [
                        new CosmosDbAccountDto
                        {
                            Name = accountName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subscriptionId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            Kind = "GlobalDocumentDB",
                            ProvisioningState = "Succeeded"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateCosmosDbAccount(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = Render<CosmosDbAccounts>();

        await cut.WaitForAssertionAsync(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No Cosmos DB accounts found")));

        await cut.Find("button.btn-primary").ClickAsync();

        await cut.Find("select").ChangeAsync(subscriptionId.ToString("D"));

        await cut.WaitForAssertionAsync(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        var selects = cut.FindAll("select");
        await selects[1].ChangeAsync("rg1");

        await cut.Find("input[placeholder='e.g. my-cosmos-account']").ChangeAsync(accountName);

        await cut.Find("button.btn-success").ClickAsync();

        await cut.WaitForAssertionAsync(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(accountName)), Is.True,
                "Expected the new Cosmos DB account name to appear in the table.");
        });

        await _client.Received(1).CreateCosmosDbAccount(
            Arg.Is<Guid>(g => g == subscriptionId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == accountName),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
