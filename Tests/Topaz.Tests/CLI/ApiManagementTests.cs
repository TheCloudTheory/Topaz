using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ApiManagementTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    private const string SubscriptionName = "apim-sub";
    private const string ResourceGroupName = "apim-rg";
    private const string ServiceName = "my-apim";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);

        await Program.RunAsync([
            "apim", "delete",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "apim", "create",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "admin@example.com",
            "--publisher-name", "Test Publisher"
        ]);
    }

    private static string MetadataPath => Path.Combine(
        Directory.GetCurrentDirectory(), ".topaz", ".subscription",
        SubscriptionId.ToString(), ".resource-group", ResourceGroupName,
        ".apim", ServiceName, "metadata.json");

    [Test]
    public void ApiManagement_Create_ResourceIsPersistedToDisk()
    {
        Assert.That(File.Exists(MetadataPath), Is.True);
    }

    [Test]
    public async Task ApiManagement_Show_ReturnsExistingService()
    {
        var code = await Program.RunAsync([
            "apim", "show",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_List_ByResourceGroup_ReturnsServices()
    {
        var code = await Program.RunAsync([
            "apim", "list",
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_List_BySubscription_ReturnsServices()
    {
        var code = await Program.RunAsync([
            "apim", "list",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Update_ReturnsUpdatedService()
    {
        var code = await Program.RunAsync([
            "apim", "update",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "updated@example.com",
            "--publisher-name", "Updated Publisher"
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_CheckName_WhenNameIsTaken_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "check-name",
            "--name", ServiceName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_CheckName_WhenNameIsAvailable_CommandSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "check-name",
            "--name", "nonexistent-apim-service",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.Zero);
    }

    [Test]
    public async Task ApiManagement_Delete_RemovesResourceFromDisk()
    {
        var code = await Program.RunAsync([
            "apim", "delete",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.Zero);
            Assert.That(File.Exists(MetadataPath), Is.False);
        });
    }

    [Test]
    public async Task ApiManagement_Create_WhenServiceAlreadyExists_UpdatesAndSucceeds()
    {
        var code = await Program.RunAsync([
            "apim", "create",
            "--name", ServiceName,
            "--resource-group", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString(),
            "--publisher-email", "updated@example.com",
            "--publisher-name", "Updated Publisher"
        ]);

        Assert.That(code, Is.Zero);
    }
}
