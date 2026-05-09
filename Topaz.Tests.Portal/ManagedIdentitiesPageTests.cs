using Topaz.Portal.Components.Pages.ManagedIdentity;
using Topaz.Portal.Models.ManagedIdentities;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ManagedIdentitiesPage_EmptyList_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ManagedIdentitiesPage_EmptyList_ShowsNoIdentitiesMessage()
    {
        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse { Value = [] }));
        _client.ListManagedIdentities()
            .Returns(Task.FromResult(new ListManagedIdentitiesResponse { Value = [] }));

        var cut = RenderComponent<ManagedIdentities>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No managed identities found")));
    }
}

[TestFixture]
public class ManagedIdentitiesPage_WithIdentities_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public void ManagedIdentitiesPage_WithIdentities_ShowsTable()
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
        _client.ListManagedIdentities()
            .Returns(Task.FromResult(new ListManagedIdentitiesResponse
            {
                Value =
                [
                    new ManagedIdentityDto
                    {
                        Id = $"/subscriptions/{subId}/resourceGroups/rg1/providers/Microsoft.ManagedIdentity/userAssignedIdentities/my-identity",
                        Name = "my-identity",
                        ResourceGroupName = "rg1",
                        SubscriptionId = subId.ToString("D"),
                        SubscriptionName = "Dev",
                        Location = "westeurope",
                        ClientId = Guid.NewGuid().ToString()
                    }
                ]
            }));

        var cut = RenderComponent<ManagedIdentities>();

        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains("my-identity")), Is.True,
                "Expected the managed identity name to appear in the table.");
        });
    }
}

[TestFixture]
public class ManagedIdentitiesPage_Create_Tests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task ManagedIdentitiesPage_CreatePanel_CreatesAndRefreshesList()
    {
        var subId = Guid.NewGuid();
        const string identityName = "new-identity";

        _client.ListSubscriptions()
            .Returns(Task.FromResult(new ListSubscriptionsResponse
            {
                Value =
                [
                    new SubscriptionDto { SubscriptionId = subId.ToString("D"), DisplayName = "Dev" }
                ]
            }));

        _client.ListManagedIdentities()
            .Returns(
                Task.FromResult(new ListManagedIdentitiesResponse { Value = [] }),
                Task.FromResult(new ListManagedIdentitiesResponse
                {
                    Value =
                    [
                        new ManagedIdentityDto
                        {
                            Name = identityName,
                            ResourceGroupName = "rg1",
                            SubscriptionId = subId.ToString("D"),
                            SubscriptionName = "Dev",
                            Location = "westeurope"
                        }
                    ]
                }));

        _client.ListResourceGroups(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ListResourceGroupsResponse
            {
                Value = [new ResourceGroupDto { Name = "rg1" }]
            }));

        _client.CreateManagedIdentity(
                Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var cut = RenderComponent<ManagedIdentities>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p").TextContent, Does.Contain("No managed identities found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select subscription
        cut.Find("select").Change(subId.ToString("D"));

        // Wait for resource group dropdown
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.GreaterThanOrEqualTo(2)));

        // Select resource group
        cut.FindAll("select")[1].Change("rg1");

        // Fill in identity name
        cut.Find("input[placeholder='e.g. my-managed-identity']").Change(identityName);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert new identity appears in the list
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(identityName)), Is.True,
                "Expected the new identity name to appear in the table.");
        });

        await _client.Received(1).CreateManagedIdentity(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == "rg1"),
            Arg.Is<string>(s => s == identityName),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
