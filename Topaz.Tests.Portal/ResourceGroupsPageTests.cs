using Topaz.Portal.Components.Pages;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ResourceGroupsPageTests : BunitTestContext
{
    private ITopazClient _client = default!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task NewResourceGroup_AppearsInList_AfterCreation()
    {
        // Arrange
        var subId = Guid.NewGuid();
        const string subName = "Dev Subscription";
        const string rgName = "rg-dev";
        const string rgLocation = "westeurope";

        var subscription = new SubscriptionDto
        {
            Id = $"/subscriptions/{subId:D}",
            SubscriptionId = subId.ToString("D"),
            DisplayName = subName
        };

        var emptyRgResponse = new ListResourceGroupsResponse { Value = [] };
        var populatedRgResponse = new ListResourceGroupsResponse
        {
            Value =
            [
                new ResourceGroupDto
                {
                    Id = $"/subscriptions/{subId:D}/resourceGroups/{rgName}",
                    Name = rgName,
                    Location = rgLocation,
                    SubscriptionId = subId.ToString("D"),
                    SubscriptionName = subName
                }
            ]
        };

        // First LoadAsync (initial): empty RGs, one subscription
        // Second LoadAsync (after creation): same subscription, populated RGs
        _client.ListSubscriptions()
            .Returns(
                Task.FromResult(new ListSubscriptionsResponse { Value = [subscription] }),
                Task.FromResult(new ListSubscriptionsResponse { Value = [subscription] }));

        _client.ListResourceGroups()
            .Returns(
                Task.FromResult(emptyRgResponse),
                Task.FromResult(populatedRgResponse));

        _client.CreateResourceGroup(default, default!, default!, default)
            .ReturnsForAnyArgs(Task.CompletedTask);

        var cut = RenderComponent<ResourceGroups>();

        // Wait for initial load
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p.mb-0").TextContent, Does.Contain("No resource groups found")));

        // Open create panel
        cut.Find("button.btn-primary").Click();

        // Select the subscription from the dropdown — re-query after re-render
        cut.Find("select.form-select").Change(subId.ToString("D"));

        // Fill in name and location
        cut.Find("input[placeholder='e.g. rg-dev']").Change(rgName);
        cut.Find("input[placeholder='e.g. westeurope']").Change(rgLocation);

        // Submit
        cut.Find("button.btn-success").Click();

        // Assert the new resource group row appears
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(rgName)), Is.True,
                "Expected the new resource group name to appear in the table.");
        });

        await _client.Received(1).CreateResourceGroup(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == rgName),
            Arg.Is<string>(s => s == rgLocation),
            Arg.Any<CancellationToken>());
    }
}
