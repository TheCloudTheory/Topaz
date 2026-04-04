using Topaz.Portal.Components.Pages;

namespace Topaz.Tests.Portal;

[TestFixture]
public class SubscriptionsPageTests : BunitTestContext
{
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task NewSubscription_AppearsInList_AfterCreation()
    {
        // Arrange: start with an empty list
        var newId = Guid.NewGuid();
        const string newName = "My Dev Subscription";

        _client.ListSubscriptions()
            .Returns(
                Task.FromResult(new ListSubscriptionsResponse { Value = [] }),
                Task.FromResult(new ListSubscriptionsResponse
                {
                    Value =
                    [
                        new SubscriptionDto
                        {
                            Id = $"/subscriptions/{newId:D}",
                            SubscriptionId = newId.ToString("D"),
                            DisplayName = newName
                        }
                    ]
                }));

        _client.CreateSubscription(Guid.Empty, null!, CancellationToken.None)
            .ReturnsForAnyArgs(Task.CompletedTask);

        var cut = RenderComponent<Subscriptions>();

        // Assert initial empty state
        cut.WaitForAssertion(() => Assert.That(cut.Find("p").TextContent, Does.Contain("No subscriptions found")));

        // Act: open the create panel and fill in the form
        cut.Find("button.btn-primary").Click();

        // Re-query after each state change so event handler IDs are fresh
        cut.Find("input[placeholder='e.g. Dev Subscription']").Change(newName);
        cut.Find("input[placeholder='leave empty to auto-generate']").Change(newId.ToString("D"));

        cut.Find("button.btn-success").Click();

        // Assert: the new subscription row appears in the table
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(newName)), Is.True,
                "Expected the new subscription display name to appear in the table.");
        });

        await _client.Received(1).CreateSubscription(
            Arg.Is<Guid>(g => g == newId),
            Arg.Is<string>(s => s == newName),
            Arg.Any<CancellationToken>());
    }
}
