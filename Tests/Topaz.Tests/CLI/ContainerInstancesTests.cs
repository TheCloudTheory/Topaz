using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ContainerInstancesTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    private const string SubscriptionName = "aci-sub";
    private const string ResourceGroupName = "aci-rg";
    private const string ContainerGroupName = "aci-test-group";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "containerinstances", "group", "delete",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "containerinstances", "group", "create",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task ContainerInstances_Create_ReturnsSuccess()
    {
        var name = $"aci-create-{Guid.NewGuid():N}";
        var code = await Program.RunAsync([
            "containerinstances", "group", "create",
            "--name", name,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Get_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "get",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Get_WhenGroupDoesNotExist_ReturnsFail()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "get",
            "--name", "nonexistent-group",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Not.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_List_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_ListAll_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "list-all",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Update_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "update",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Start_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "start",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Stop_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "stop",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Restart_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "restart",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerInstances_Delete_WhenGroupExists_ReturnsSuccess()
    {
        var code = await Program.RunAsync([
            "containerinstances", "group", "delete",
            "--name", ContainerGroupName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }
}
