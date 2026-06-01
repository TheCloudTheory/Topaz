namespace Topaz.Tests.AzureCLI;

public class CosmosDbTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-cosmosdb";
    private const string AccountName = "my-cli-cosmos-account";

    [Test]
    public async Task CosmosDbTests_WhenAccountIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {AccountName} --resource-group {ResourceGroup} --location westeurope",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(AccountName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.DocumentDB/databaseAccounts").IgnoreCase);
                    Assert.That(response["documentEndpoint"]!.GetValue<string>(),
                        Does.Contain(".documents.topaz.local.dev"));
                });
            }, 0);
    }

    [Test]
    public async Task CosmosDbTests_WhenAccountIsDeleted_ItShouldNotBeAvailable()
    {
        var rgDel = $"{ResourceGroup}-del";
        var accountDel = $"{AccountName}-del";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgDel}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {accountDel} --resource-group {rgDel} --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb delete --name {accountDel} --resource-group {rgDel} --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb show --name {accountDel} --resource-group {rgDel}",
            null, 3);
    }

    [Test]
    public async Task CosmosDbTests_WhenAccountsAreListed_AllShouldAppear()
    {
        var rgList = $"{ResourceGroup}-list";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgList}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {AccountName}-list-a --resource-group {rgList} --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {AccountName}-list-b --resource-group {rgList} --location westeurope",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb list --resource-group {rgList}",
            response =>
            {
                var names = response.AsArray()!.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.That(names, Does.Contain($"{AccountName}-list-a"));
                Assert.That(names, Does.Contain($"{AccountName}-list-b"));
            }, 0);
    }
}
