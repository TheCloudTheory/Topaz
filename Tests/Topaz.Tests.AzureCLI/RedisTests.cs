namespace Topaz.Tests.AzureCLI;

public class RedisTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-redis";
    private const string CacheName = "cache-cli-test";

    [Test]
    public async Task RedisTests_WhenRedisIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName} --resource-group {ResourceGroup} --location westeurope --sku Basic --vm-size c0",
            null, 0);
        await RunAzureCliCommand(
            $"az redis show --name {CacheName} --resource-group {ResourceGroup}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(CacheName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Cache/Redis").IgnoreCase);
                });
            }, 0);
    }

    [Test]
    public async Task RedisTests_WhenRedisIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName}-del --resource-group {ResourceGroup}-del --location westeurope --sku Basic --vm-size c0",
            null, 0);
        await RunAzureCliCommand(
            $"az redis delete --name {CacheName}-del --resource-group {ResourceGroup}-del --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az redis show --name {CacheName}-del --resource-group {ResourceGroup}-del",
            null, 3);
    }

    [Test]
    public async Task RedisTests_WhenRedisAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName}-list-a --resource-group {ResourceGroup}-list --location westeurope --sku Basic --vm-size c0",
            null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName}-list-b --resource-group {ResourceGroup}-list --location westeurope --sku Basic --vm-size c0",
            null, 0);
        await RunAzureCliCommand(
            $"az redis list --resource-group {ResourceGroup}-list",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{CacheName}-list-a"));
                    Assert.That(names, Does.Contain($"{CacheName}-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task RedisTests_WhenListKeysIsCalled_ItShouldReturnKeys()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-keys", null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName}-keys --resource-group {ResourceGroup}-keys --location westeurope --sku Basic --vm-size c0",
            null, 0);
        await RunAzureCliCommand(
            $"az redis list-keys --name {CacheName}-keys --resource-group {ResourceGroup}-keys",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["primaryKey"]!.GetValue<string>(), Is.Not.Empty);
                    Assert.That(response["secondaryKey"]!.GetValue<string>(), Is.Not.Empty);
                });
            }, 0);
    }

    [Test]
    public async Task RedisTests_WhenRegenerateKeysIsCalled_PrimaryKeyShouldChange()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-regen", null, 0);
        await RunAzureCliCommand(
            $"az redis create --name {CacheName}-regen --resource-group {ResourceGroup}-regen --location westeurope --sku Basic --vm-size c0",
            null, 0);
        string? originalKey = null;
        await RunAzureCliCommand(
            $"az redis list-keys --name {CacheName}-regen --resource-group {ResourceGroup}-regen",
            response =>
            {
                originalKey = response["primaryKey"]!.GetValue<string>();
            }, 0);
        await RunAzureCliCommand(
            $"az redis regenerate-keys --name {CacheName}-regen --resource-group {ResourceGroup}-regen --key-type Primary",
            response =>
            {
                Assert.That(response["primaryKey"]!.GetValue<string>(), Is.Not.EqualTo(originalKey));
            }, 0);
    }
}
