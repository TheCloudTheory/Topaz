using Topaz.Portal.Components.Pages.ResourceManager;
using Topaz.Portal.Models.ManagementGroups;

namespace Topaz.Tests.Portal;

[TestFixture]
public class ManagementGroupsPageTests
{
    private Bunit.TestContext _ctx = null!;
    private ITopazClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new Bunit.TestContext();
        _client = Substitute.For<ITopazClient>();
        _ctx.Services.AddSingleton(_client);
    }

    [TearDown]
    public void TearDown() => _ctx.Dispose();

    [Test]
    public void EmptyState_ShowsNoGroupsMessage()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(
                new GetManagementGroupEntitiesResponse { Value = [] }));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain("No management groups found")));
    }

    [Test]
    public void TreeRender_ShowsRootGroupsAndChildren()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(new GetManagementGroupEntitiesResponse
            {
                Value =
                [
                    new ManagementGroupEntityDto
                    {
                        Id = "/providers/Microsoft.Management/managementGroups/root-mg",
                        Type = "Microsoft.Management/managementGroups",
                        Name = "root-mg",
                        DisplayName = "Root Management Group",
                        ParentId = null
                    },
                    new ManagementGroupEntityDto
                    {
                        Id = "/providers/Microsoft.Management/managementGroups/child-mg",
                        Type = "Microsoft.Management/managementGroups",
                        Name = "child-mg",
                        DisplayName = "Child Management Group",
                        ParentId = "/providers/Microsoft.Management/managementGroups/root-mg"
                    },
                    new ManagementGroupEntityDto
                    {
                        Id = "/subscriptions/aaaaaaaa-0000-0000-0000-000000000001",
                        Type = "/subscriptions",
                        Name = "aaaaaaaa-0000-0000-0000-000000000001",
                        DisplayName = "Dev Subscription",
                        ParentId = "/providers/Microsoft.Management/managementGroups/root-mg"
                    }
                ]
            }));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.Markup, Does.Contain("Root Management Group"));
            Assert.That(cut.Markup, Does.Contain("Child Management Group"));
            Assert.That(cut.Markup, Does.Contain("Dev Subscription"));
        });
    }

    [Test]
    public void NewManagementGroupButton_TogglesPanel()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(
                new GetManagementGroupEntitiesResponse { Value = [] }));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("No management groups found")));

        Assert.That(cut.Markup, Does.Not.Contain("Group ID"));

        cut.Find("button.btn-outline-primary").Click();

        Assert.That(cut.Markup, Does.Contain("Group ID"));
    }

    [Test]
    public void NewSubscriptionButton_TogglesPanel()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(
                new GetManagementGroupEntitiesResponse { Value = [] }));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("No management groups found")));

        cut.Find("button.btn-outline-secondary").Click();

        Assert.That(cut.Markup, Does.Contain("Display name"));
        Assert.That(cut.Markup, Does.Contain("Subscription ID"));
    }

    [Test]
    public async Task CreateManagementGroup_CallsClientAndRefreshes()
    {
        const string groupId = "my-new-group";
        const string displayName = "My New Group";

        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(
                Task.FromResult(new GetManagementGroupEntitiesResponse { Value = [] }),
                Task.FromResult(new GetManagementGroupEntitiesResponse
                {
                    Value =
                    [
                        new ManagementGroupEntityDto
                        {
                            Id = $"/providers/Microsoft.Management/managementGroups/{groupId}",
                            Type = "Microsoft.Management/managementGroups",
                            Name = groupId,
                            DisplayName = displayName,
                            ParentId = null
                        }
                    ]
                }));

        _client.CreateManagementGroup(null!, null!, null, CancellationToken.None)
            .ReturnsForAnyArgs(Task.CompletedTask);

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("No management groups found")));

        cut.Find("button.btn-outline-primary").Click();
        cut.Find("input[placeholder='e.g. my-group']").Change(groupId);
        cut.Find("input[placeholder='e.g. My Group']").Change(displayName);
        cut.Find("button.btn-primary").Click();

        await _client.Received(1).CreateManagementGroup(
            Arg.Is<string>(s => s == groupId),
            Arg.Is<string>(s => s == displayName),
            Arg.Is<string?>(s => s == null),
            Arg.Any<CancellationToken>());

        cut.WaitForAssertion(() =>
            Assert.That(cut.Markup, Does.Contain(displayName)));
    }

    [Test]
    public void CreateGroupValidation_ShowsError_WhenIdMissing()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs(Task.FromResult(
                new GetManagementGroupEntitiesResponse { Value = [] }));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("No management groups found")));

        cut.Find("button.btn-outline-primary").Click();
        cut.Find("button.btn-primary").Click();

        Assert.That(cut.Markup, Does.Contain("Please enter a group ID"));
    }

    [Test]
    public void LoadError_ShowsAlertDanger()
    {
        _client.GetManagementGroupEntities(CancellationToken.None)
            .ReturnsForAnyArgs<GetManagementGroupEntitiesResponse>(_ =>
                throw new InvalidOperationException("service unavailable"));

        var cut = _ctx.RenderComponent<ManagementGroupsPage>();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("service unavailable")));
    }
}


