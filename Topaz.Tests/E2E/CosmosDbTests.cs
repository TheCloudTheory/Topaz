using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;
namespace Topaz.Tests.E2E;

public class CosmosDbTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC002200");

    private const string SubscriptionName = "sub-test-cosmosdb";
    private const string ResourceGroupName = "rg-test-cosmosdb";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);
    }

    private static CosmosDBAccountCreateOrUpdateContent MinimalAccountContent() =>
        new(AzureLocation.WestEurope, [new CosmosDBAccountLocation { LocationName = "westeurope" }]);

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    [Test]
    public async Task DatabaseAccount_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-create";

        // Act
        var createResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var account = createResult.Value;

        // Assert
        Assert.That(account, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(account.Data.Name, Is.EqualTo(accountName));
            Assert.That(account.Data.ResourceType.ToString(), Is.EqualTo("Microsoft.DocumentDB/databaseAccounts").IgnoreCase);
            Assert.That(account.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(account.Data.DocumentEndpoint, Does.Contain($"{accountName}.documents.topaz.local.dev"));
        });
    }

    [Test]
    public async Task DatabaseAccount_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-get";

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        // Act
        var getResult = await resourceGroup.Value.GetCosmosDBAccountAsync(accountName);

        // Assert
        Assert.That(getResult.Value, Is.Not.Null);
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(accountName));
    }

    [Test]
    public async Task DatabaseAccount_WhenUpdated_HasNewTags()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-update";

        var createContent = MinimalAccountContent();
        createContent.Tags.Add("env", "test");
        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, createContent);

        // Act
        var updateContent = MinimalAccountContent();
        updateContent.Tags.Add("env", "updated");
        var updateResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, updateContent);

        // Assert
        Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("updated"));
    }

    [Test]
    public async Task DatabaseAccount_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-delete";

        var createResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        // Act
        await createResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetCosmosDBAccountAsync(accountName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task DatabaseAccount_WhenListedByResourceGroup_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-list-a", MinimalAccountContent());
        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-list-b", MinimalAccountContent());

        // Act
        var accounts = resourceGroup.Value.GetCosmosDBAccounts().GetAllAsync();
        var names = new List<string>();
        await foreach (var account in accounts)
        {
            names.Add(account.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-cosmos-list-a"));
        Assert.That(names, Does.Contain("test-cosmos-list-b"));
    }

    [Test]
    public async Task DatabaseAccount_WhenListedBySubscription_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-cosmos-sub-list-a", MinimalAccountContent());

        // Act
        var accounts = subscription.GetCosmosDBAccountsAsync();
        var names = new List<string>();
        await foreach (var account in accounts)
        {
            names.Add(account.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-cosmos-sub-list-a"));
    }

    [Test]
    public async Task SqlDatabase_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-create";
        const string databaseName = "mydb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var createContent = new CosmosDBSqlDatabaseCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlDatabaseResourceInfo(databaseName))
        {
            Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
        };

        // Act
        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName, createContent);

        // Assert
        Assert.That(dbResult.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(dbResult.Value.Data.Name, Is.EqualTo(databaseName));
            Assert.That(dbResult.Value.Data.ResourceType.ToString(),
                Is.EqualTo("Microsoft.DocumentDB/databaseAccounts/sqlDatabases").IgnoreCase);
            Assert.That(dbResult.Value.Data.Resource.DatabaseName, Is.EqualTo(databaseName));
        });
    }

    [Test]
    public async Task SqlDatabase_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-get";
        const string databaseName = "getdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        // Act
        var getResult = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);

        // Assert
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(databaseName));
    }

    [Test]
    public async Task SqlDatabase_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-delete";
        const string databaseName = "deletedb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        // Act
        await dbResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task SqlDatabase_WhenListed_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-list";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listdb-a",
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo("listdb-a")));
        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, "listdb-b",
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo("listdb-b")));

        // Act
        var databases = accountResult.Value.GetCosmosDBSqlDatabases().GetAllAsync();
        var names = new List<string>();
        await foreach (var db in databases)
        {
            names.Add(db.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("listdb-a"));
        Assert.That(names, Does.Contain("listdb-b"));
    }

    [Test]
    public async Task SqlDatabase_WhenThroughputUpdated_ReflectsNewValue()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-throughput";
        const string databaseName = "throughputdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        var dbResult = await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName))
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
                });

        // Act
        var updateContent = new ThroughputSettingsUpdateData(
            AzureLocation.WestEurope,
            new ThroughputSettingsResourceInfo { Throughput = 800 });
        var throughputResult = await dbResult.Value.GetCosmosDBSqlDatabaseThroughputSetting()
            .CreateOrUpdateAsync(WaitUntil.Completed, updateContent);

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(800));
    }

    [Test]
    public async Task SqlDatabase_WhenThroughputFetched_ReturnsThroughputSettings()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqldb-getthroughput";
        const string databaseName = "getthroughputdb";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName))
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 600 }
                });

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);

        // Act
        var throughputResult = await database.Value.GetCosmosDBSqlDatabaseThroughputSetting().GetAsync();

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(600));
    }

    [Test]
    public async Task DatabaseAccount_WhenPrimaryKeyRegenerated_PrimaryKeyChanges()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-regen-key";

        var createResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());
        var account = createResult.Value;

        var keysBefore = await account.GetKeysAsync();
        var primaryKeyBefore = keysBefore.Value.PrimaryMasterKey;
        var secondaryKeyBefore = keysBefore.Value.SecondaryMasterKey;

        // Act
        await account.RegenerateKeyAsync(
            WaitUntil.Completed,
            new CosmosDBAccountRegenerateKeyContent(CosmosDBAccountKeyKind.Primary));

        // Assert
        var keysAfter = await account.GetKeysAsync();
        Assert.Multiple(() =>
        {
            Assert.That(keysAfter.Value.PrimaryMasterKey, Is.Not.EqualTo(primaryKeyBefore));
            Assert.That(keysAfter.Value.SecondaryMasterKey, Is.EqualTo(secondaryKeyBefore));
        });
    }

    [Test]
    public async Task SqlContainer_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-create";
        const string databaseName = "mydb-ctr";
        const string containerName = "mycontainer";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var createContent = new CosmosDBSqlContainerCreateOrUpdateContent(
            AzureLocation.WestEurope,
            new CosmosDBSqlContainerResourceInfo(containerName)
            {
                PartitionKey = new CosmosDBContainerPartitionKey
                {
                    Kind = CosmosDBPartitionKind.Hash,
                    Paths = { "/pk" }
                }
            })
        {
            Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
        };

        // Act
        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);
        var containerResult = await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName, createContent);

        // Assert
        Assert.That(containerResult.Value, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(containerResult.Value.Data.Name, Is.EqualTo(containerName));
            Assert.That(containerResult.Value.Data.ResourceType.ToString(),
                Is.EqualTo("Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers").IgnoreCase);
            Assert.That(containerResult.Value.Data.Resource.ContainerName, Is.EqualTo(containerName));
        });
    }

    [Test]
    public async Task SqlContainer_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-get";
        const string databaseName = "getdb-ctr";
        const string containerName = "getcontainer";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);
        await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName,
                new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlContainerResourceInfo(containerName)
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Kind = CosmosDBPartitionKind.Hash, Paths = { "/pk" } }
                    }));

        // Act
        var getResult = await database.Value.GetCosmosDBSqlContainerAsync(containerName);

        // Assert
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(containerName));
    }

    [Test]
    public async Task SqlContainer_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-delete";
        const string databaseName = "deletedb-ctr";
        const string containerName = "deletecontainer";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);
        var containerResult = await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName,
                new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlContainerResourceInfo(containerName)
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Kind = CosmosDBPartitionKind.Hash, Paths = { "/pk" } }
                    }));

        // Act
        await containerResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await database.Value.GetCosmosDBSqlContainerAsync(containerName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task SqlContainer_WhenListed_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-list";
        const string databaseName = "listdb-ctr";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);

        foreach (var name in new[] { "ctr-a", "ctr-b" })
        {
            await database.Value.GetCosmosDBSqlContainers()
                .CreateOrUpdateAsync(WaitUntil.Completed, name,
                    new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                        new CosmosDBSqlContainerResourceInfo(name)
                        {
                            PartitionKey = new CosmosDBContainerPartitionKey { Kind = CosmosDBPartitionKind.Hash, Paths = { "/pk" } }
                        }));
        }

        // Act
        var containers = database.Value.GetCosmosDBSqlContainers().GetAllAsync();
        var names = new List<string>();
        await foreach (var ctr in containers)
        {
            names.Add(ctr.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("ctr-a"));
        Assert.That(names, Does.Contain("ctr-b"));
    }

    [Test]
    public async Task SqlContainer_WhenThroughputUpdated_ReflectsNewValue()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-throughput";
        const string databaseName = "throughputdb-ctr";
        const string containerName = "throughputcontainer";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);
        var containerResult = await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName,
                new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlContainerResourceInfo(containerName)
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Kind = CosmosDBPartitionKind.Hash, Paths = { "/pk" } }
                    })
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 400 }
                });

        // Act
        var updateContent = new ThroughputSettingsUpdateData(
            AzureLocation.WestEurope,
            new ThroughputSettingsResourceInfo { Throughput = 800 });
        var throughputResult = await containerResult.Value.GetCosmosDBSqlContainerThroughputSetting()
            .CreateOrUpdateAsync(WaitUntil.Completed, updateContent);

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(800));
    }

    [Test]
    public async Task SqlContainer_WhenThroughputFetched_ReturnsThroughputSettings()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string accountName = "test-cosmos-sqlctr-getthroughput";
        const string databaseName = "getthroughputdb-ctr";
        const string containerName = "getthroughputcontainer";

        var accountResult = await resourceGroup.Value.GetCosmosDBAccounts()
            .CreateOrUpdateAsync(WaitUntil.Completed, accountName, MinimalAccountContent());

        await accountResult.Value.GetCosmosDBSqlDatabases()
            .CreateOrUpdateAsync(WaitUntil.Completed, databaseName,
                new CosmosDBSqlDatabaseCreateOrUpdateContent(AzureLocation.WestEurope, new CosmosDBSqlDatabaseResourceInfo(databaseName)));

        var database = await accountResult.Value.GetCosmosDBSqlDatabaseAsync(databaseName);
        await database.Value.GetCosmosDBSqlContainers()
            .CreateOrUpdateAsync(WaitUntil.Completed, containerName,
                new CosmosDBSqlContainerCreateOrUpdateContent(AzureLocation.WestEurope,
                    new CosmosDBSqlContainerResourceInfo(containerName)
                    {
                        PartitionKey = new CosmosDBContainerPartitionKey { Kind = CosmosDBPartitionKind.Hash, Paths = { "/pk" } }
                    })
                {
                    Options = new CosmosDBCreateUpdateConfig { Throughput = 600 }
                });

        var container = await database.Value.GetCosmosDBSqlContainerAsync(containerName);

        // Act
        var throughputResult = await container.Value.GetCosmosDBSqlContainerThroughputSetting().GetAsync();

        // Assert
        Assert.That(throughputResult.Value.Data.Resource.Throughput, Is.EqualTo(600));
    }

    [Test]
    public async Task DatabaseAccount_WhenDeployedViaArmTemplate_PopulatesLocations()
    {
        // Arrange
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, "rg-cosmos-locations",
            new ResourceGroupData(AzureLocation.WestEurope));

        const string accountName = "test-cosmos-arm-locations";

        var parameters = BinaryData.FromString(JsonSerializer.Serialize(new
        {
            accountName = new { value = accountName },
            secondaryLocation = new { value = "northeurope" }
        }));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-cosmos-locations",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync(
                    Path.Combine(AppContext.BaseDirectory, "templates", "deployment-cosmos-locations.json"))),
                Parameters = parameters
            }));

        var account = await rg.Value.GetCosmosDBAccountAsync(accountName);

        // Assert
        Assert.That(account.Value.Data.Locations, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(account.Value.Data.Locations.Any(l =>
                    string.Equals(l.LocationName, "westeurope", StringComparison.OrdinalIgnoreCase) &&
                    l.FailoverPriority == 0),
                Is.True, "Expected westeurope as primary (failoverPriority=0)");
            Assert.That(account.Value.Data.Locations.Any(l =>
                    string.Equals(l.LocationName, "northeurope", StringComparison.OrdinalIgnoreCase) &&
                    l.FailoverPriority == 1),
                Is.True, "Expected northeurope as secondary (failoverPriority=1)");
        }
    }
}
