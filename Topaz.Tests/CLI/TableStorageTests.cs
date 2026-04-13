using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class TableStorageTests
{
    private static readonly Guid SubscriptionId = Guid.NewGuid();
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string StorageAccountName = "testsa";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            "test",
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "storage",
            "account",
            "delete",
            "--name",
            "test",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "storage",
            "account",
            "create",
            "--name",
            "test",
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }
    
    [Test]
    public async Task TableStorageTests_WhenNewTableIsRequested_ItShouldBeCreated()
    {
        var tableDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", "test", ".table", "test", "metadata.json");
        
        await Program.RunAsync([
            "storage",
            "table",
            "create",
            "--name",
            "test",
            "--account-name",
            "test",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(tableDirectoryPath), Is.True);
    }
    
    [Test]
    public async Task TableStorageTests_WhenNewTableIsDeleted_ItShouldBeDeleted()
    {
        var tableDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", "test", ".table", "test", "metadata.json");
        
        await Program.RunAsync([
            "storage",
            "table",
            "create",
            "--name",
            "test",
            "--account-name",
            "test",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync([
            "storage",
            "table",
            "delete",
            "--name",
            "test",
            "--account-name",
            "test",
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(File.Exists(tableDirectoryPath), Is.False);
    }
}