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
            $"az cosmosdb create --name {AccountName} --resource-group {ResourceGroup} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
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
            $"az cosmosdb create --name {accountDel} --resource-group {rgDel} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
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
            $"az cosmosdb create --name {AccountName}-list-a --resource-group {rgList} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {AccountName}-list-b --resource-group {rgList} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
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

    [Test]
    public async Task SqlDatabase_WhenCreated_ItShouldBeAvailable()
    {
        var rgDb = $"{ResourceGroup}-sqldb-create";
        var accountDb = $"{AccountName}-sqldb-create";
        const string databaseName = "cli-mydb";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgDb}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {accountDb} --resource-group {rgDb} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database create --account-name {accountDb} --resource-group {rgDb} --name {databaseName}",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(databaseName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.DocumentDB/databaseAccounts/sqlDatabases").IgnoreCase);
                });
            }, 0);
    }

    [Test]
    public async Task SqlDatabase_WhenDeleted_ItShouldNotBeAvailable()
    {
        var rgDb = $"{ResourceGroup}-sqldb-delete";
        var accountDb = $"{AccountName}-sqldb-delete";
        const string databaseName = "cli-deletedb";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgDb}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {accountDb} --resource-group {rgDb} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database create --account-name {accountDb} --resource-group {rgDb} --name {databaseName}",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database delete --account-name {accountDb} --resource-group {rgDb} --name {databaseName} --yes",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database show --account-name {accountDb} --resource-group {rgDb} --name {databaseName}",
            null, 3);
    }

    [Test]
    public async Task SqlDatabase_WhenListed_AllShouldAppear()
    {
        var rgDb = $"{ResourceGroup}-sqldb-list";
        var accountDb = $"{AccountName}-sqldb-list";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgDb}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {accountDb} --resource-group {rgDb} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database create --account-name {accountDb} --resource-group {rgDb} --name cli-listdb-a",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database create --account-name {accountDb} --resource-group {rgDb} --name cli-listdb-b",
            null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb sql database list --account-name {accountDb} --resource-group {rgDb}",
            response =>
            {
                var names = response.AsArray()!.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.That(names, Does.Contain("cli-listdb-a"));
                Assert.That(names, Does.Contain("cli-listdb-b"));
            }, 0);
    }

    [Test]
    public async Task CosmosDbTests_WhenKeyIsRegenerated_NewKeyIsDifferent()
    {
        var rgRegen = $"{ResourceGroup}-regen-key";
        var accountRegen = $"{AccountName}-regen";
        await RunAzureCliCommand($"az group create -l westeurope -n {rgRegen}", null, 0);
        await RunAzureCliCommand(
            $"az cosmosdb create --name {accountRegen} --resource-group {rgRegen} --locations regionName=westeurope failoverPriority=0 isZoneRedundant=False",
            null, 0);

        string? primaryKeyBefore = null;
        await RunAzureCliCommand(
            $"az cosmosdb keys list --name {accountRegen} --resource-group {rgRegen} --type keys",
            response =>
            {
                primaryKeyBefore = response["primaryMasterKey"]!.GetValue<string>();
                Assert.That(primaryKeyBefore, Is.Not.Null.And.Not.Empty);
            }, 0);

        await RunAzureCliCommand(
            $"az cosmosdb keys regenerate --name {accountRegen} --resource-group {rgRegen} --key-kind primary",
            null, 0);

        await RunAzureCliCommand(
            $"az cosmosdb keys list --name {accountRegen} --resource-group {rgRegen} --type keys",
            response =>
            {
                var primaryKeyAfter = response["primaryMasterKey"]!.GetValue<string>();
                Assert.That(primaryKeyAfter, Is.Not.EqualTo(primaryKeyBefore));
            }, 0);
    }
}
