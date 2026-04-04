using Topaz.Portal.Components.Pages.ResourceGroup;
using Topaz.Portal.Models.ResourceGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ResourceGroupTagsPageTests : BunitTestContext
{
    private ITopazClient _client = default!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITopazClient>();
        Services.AddSingleton(_client);
    }

    [Test]
    public async Task AddTag_AppearsInTagList_AfterSave()
    {
        // Arrange
        var subId = Guid.NewGuid();
        const string rgName = "rg-dev";
        const string tagKey = "environment";
        const string tagValue = "production";

        var rgWithoutTags = new ResourceGroupDto
        {
            Id = $"/subscriptions/{subId:D}/resourceGroups/{rgName}",
            Name = rgName,
            Location = "westeurope",
            SubscriptionId = subId.ToString("D"),
            SubscriptionName = "Dev Subscription",
            Tags = []
        };

        var rgWithTag = new ResourceGroupDto
        {
            Id = rgWithoutTags.Id,
            Name = rgWithoutTags.Name,
            Location = rgWithoutTags.Location,
            SubscriptionId = rgWithoutTags.SubscriptionId,
            SubscriptionName = rgWithoutTags.SubscriptionName,
            Tags = new Dictionary<string, string> { { tagKey, tagValue } }
        };

        // First GetResourceGroup call: no tags. Second (after save): tag present.
        _client.GetResourceGroup(default, default!, default)
            .ReturnsForAnyArgs(
                Task.FromResult<ResourceGroupDto?>(rgWithoutTags),
                Task.FromResult<ResourceGroupDto?>(rgWithTag));

        _client.CreateOrUpdateResourceGroupTag(default, default!, default!, default!, default)
            .ReturnsForAnyArgs(Task.CompletedTask);

        var cut = RenderComponent<ResourceGroupTags>(p => p
            .Add(x => x.SubscriptionId, subId)
            .Add(x => x.ResourceGroupName, rgName));

        // Wait for initial load — no tags yet
        cut.WaitForAssertion(() =>
            Assert.That(cut.Find("p.text-muted").TextContent, Does.Contain("No tags assigned")));

        // Fill in the tag inputs — uses oninput so must use Input(), not Change()
        cut.Find("#tagsPanelName").Input(tagKey);
        cut.Find("#tagsPanelValue").Input(tagValue);

        // Click Add tag
        cut.Find("button.btn-primary").Click();

        // Assert the tag row appears
        cut.WaitForAssertion(() =>
        {
            var cells = cut.FindAll("td");
            Assert.That(cells.Any(td => td.TextContent.Contains(tagKey)), Is.True,
                "Expected tag key to appear in the tags table.");
            Assert.That(cells.Any(td => td.TextContent.Contains(tagValue)), Is.True,
                "Expected tag value to appear in the tags table.");
        });

        await _client.Received(1).CreateOrUpdateResourceGroupTag(
            Arg.Is<Guid>(g => g == subId),
            Arg.Is<string>(s => s == rgName),
            Arg.Is<string>(s => s == tagKey),
            Arg.Is<string>(s => s == tagValue),
            Arg.Any<CancellationToken>());
    }
}
