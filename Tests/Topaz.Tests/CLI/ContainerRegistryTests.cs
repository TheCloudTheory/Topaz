using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ContainerRegistryTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("E8E33E85-CB72-474D-9F4B-BC8C489D9817");
    private const string SubscriptionName = "acr-sub";
    private const string ResourceGroupName = "acr-rg";
    private const string RegistryName = "acrclitest";

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
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "acr",
            "delete",
            "--name",
            RegistryName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
            "acr",
            "create",
            "--name",
            RegistryName,
            "--resource-group",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public async Task ContainerRegistry_RepositoryList_WhenRepositoryExists_CommandShouldSucceed()
    {
        SeedRepository("sample-repo");

        var code = await Program.RunAsync([
            "acr",
            "repository",
            "list",
            "--registry",
            RegistryName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(code, Is.EqualTo(0));
    }

    [Test]
    public async Task ContainerRegistry_RepositoryDelete_WhenRepositoryExists_ItShouldBeDeleted()
    {
        const string repositoryName = "delete-me";
        var repositoryPath = SeedRepository(repositoryName);

        var code = await Program.RunAsync([
            "acr",
            "repository",
            "delete",
            "--name",
            repositoryName,
            "--registry",
            RegistryName,
            "--resource-group",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(0));
            Assert.That(Directory.Exists(repositoryPath), Is.False);
        });
    }

    private static string SeedRepository(string repositoryName)
    {
        var manifestsRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".topaz",
            ".subscription",
            SubscriptionId.ToString(),
            ".resource-group",
            ResourceGroupName,
            ".container-registry",
            RegistryName,
            "data",
            "manifests");

        var repositoryPath = Path.Combine(manifestsRoot, repositoryName);
        Directory.CreateDirectory(repositoryPath);
        File.WriteAllText(Path.Combine(repositoryPath, "v1.json"), "{}");

        return repositoryPath;
    }
}