using Topaz.Portal.Components.Pages.Storage;
using Topaz.Portal.Models.ResourceGroups;
using Topaz.Portal.Models.Storage;

namespace Topaz.Tests.Portal;

[TestFixture]
public class StorageAccountsPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void StorageAccountsPage_EmptyList_ShowsNoStorageAccountsMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListStorageAccounts()
            .Returns(Task.FromResult(new ListStorageAccountsResponse { Value = [] }));

        var cut = RenderComponent<StorageAccounts>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No storage accounts found")));
    }
}

[TestFixture]
public class StorageAccountsPage_WithAccounts_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void StorageAccountsPage_WithAccounts_ShowsTable()
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
        _client.ListStorageAccounts()
            .Returns(Task.FromResult(new ListStorageAccountsResponse
            {
                Value =
                [
                    new StorageAccountDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.Storage/storageAccounts/mystorage",
                        Name = "mystorage",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        Kind = "StorageV2",
                        SkuName = "Standard_LRS"
                    }
                ]
            }));

        var cut = RenderComponent<StorageAccounts>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("mystorage")), Is.True,
                "Expected the storage account name to appear in the table.");
        });
    }
}

[TestFixture]
public class StorageAccountsPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task StorageAccountsPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subId = Guid.NewGuid();
        const string accountName = "mynewstorage";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListStorageAccounts()
            .Returns(
                Task.FromResult(new ListStorageAccountsResponse { Value = [] }),
                Task.FromResult(new ListStorageAccountsResponse
                {
                    Value =
                    [
                        new StorageAccountDto
                        {
                            Name = accountName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope",
                            Kind = "StorageV2",
                            SkuName = "Standard_LRS"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateStorageAccount(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<StorageAccounts>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No storage accounts found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select subscription (first select)
        cut.Find("select").Change(subId.ToString("D"));

        // Wait for resource group dropdown to populate
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group (second select)
        var selects = cut.FindAll("select");
        selects[1].Change("rg1");

        // Fill in name
        cut.Find("input[placeholder='e.g. mystorageaccount']").Change(accountName);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert new account appears in the list
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(accountName)), Is.True,
                "Expected the new storage account name to appear in the table.");
        });

        await _client.Received(1).CreateStorageAccount(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == accountName),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
