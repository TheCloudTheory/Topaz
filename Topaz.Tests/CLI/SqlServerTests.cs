using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class SqlServerTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC002233");
    private const string SubscriptionName = "sql-sub";
    private const string ResourceGroupName = "sql-rg";
    private const string ServerName = "my-sql-server";

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

        await Program.RunAsync(
        [
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "sql",
            "delete",
            "--name",
            ServerName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "sql",
            "create",
            "--name",
            ServerName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString(),
            "-u",
            "sqladmin",
            "-p",
            "SqlAdmin1234!@#"
        ]);
    }

    [Test]
    public void SqlServerTests_WhenServerIsCreated_ItShouldBeStoredOnDisk()
    {
        var serverPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-sql", ServerName,
            "metadata.json");

        Assert.That(File.Exists(serverPath), Is.True);
    }

    [Test]
    public async Task SqlServerTests_WhenServerIsDeleted_ItShouldBeRemovedFromDisk()
    {
        var serverPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-sql", ServerName,
            "metadata.json");

        var result = await Program.RunAsync(
        [
            "sql",
            "delete",
            "--name",
            ServerName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(serverPath), Is.False);
        });
    }

    [Test]
    public async Task SqlServerTests_WhenServerIsShown_ItShouldReturnServerDetails()
    {
        var result = await Program.RunAsync(
        [
            "sql",
            "show",
            "--name",
            ServerName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }
}
