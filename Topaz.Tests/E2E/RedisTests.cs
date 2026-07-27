using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Redis;
using Azure.ResourceManager.Redis.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class RedisTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003300");

    private const string SubscriptionName = "sub-test-redis";
    private const string ResourceGroupName = "rg-test-redis";

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

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static RedisCreateOrUpdateContent MinimalRedisContent() =>
        new(AzureLocation.WestEurope, new RedisSku(RedisSkuName.Basic, RedisSkuFamily.BasicOrStandard, 0));

    [Test]
    public async Task Redis_WhenCreated_HasCorrectProperties()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-create";

        // Act
        var createResult = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());

        var cache = createResult.Value;

        // Assert
        Assert.That(cache, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(cache.Data.Name, Is.EqualTo(cacheName));
            Assert.That(cache.Data.ResourceType.ToString(), Is.EqualTo("Microsoft.Cache/Redis").IgnoreCase);
            Assert.That(cache.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
        }
    }

    [Test]
    public async Task Redis_WhenRetrieved_IsFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-get";

        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());

        // Act
        var getResult = await resourceGroup.Value.GetRedisAsync(cacheName);

        // Assert
        Assert.That(getResult.Value, Is.Not.Null);
        Assert.That(getResult.Value.Data.Name, Is.EqualTo(cacheName));
    }

    [Test]
    public async Task Redis_WhenUpdated_HasNewTags()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-update";

        var createContent = MinimalRedisContent();
        createContent.Tags.Add("env", "test");
        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, createContent);

        // Act
        var updateContent = MinimalRedisContent();
        updateContent.Tags.Add("env", "updated");
        var updateResult = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, updateContent);

        // Assert
        Assert.That(updateResult.Value.Data.Tags["env"], Is.EqualTo("updated"));
    }

    [Test]
    public async Task Redis_WhenDeleted_ReturnsNotFound()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-delete";

        var createResult = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());

        // Act
        await createResult.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        var notFound = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetRedisAsync(cacheName));
        Assert.That(notFound!.Status, Is.EqualTo(404));
    }

    [Test]
    public async Task Redis_WhenListedByResourceGroup_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-redis-list-a", MinimalRedisContent());
        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-redis-list-b", MinimalRedisContent());

        // Act
        var names = new List<string>();
        await foreach (var cache in resourceGroup.Value.GetAllRedis().GetAllAsync())
        {
            names.Add(cache.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-redis-list-a"));
        Assert.That(names, Does.Contain("test-redis-list-b"));
    }

    [Test]
    public async Task Redis_WhenListedBySubscription_ReturnsAll()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-redis-sub-list-a", MinimalRedisContent());

        // Act
        var names = new List<string>();
        await foreach (var cache in subscription.GetAllRedisAsync())
        {
            names.Add(cache.Data.Name);
        }

        // Assert
        Assert.That(names, Does.Contain("test-redis-sub-list-a"));
    }

    [Test]
    public async Task Redis_WhenKeysListed_ReturnsPrimaryAndSecondary()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-keys";

        var createResult = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());

        // Act
        var keys = await createResult.Value.GetKeysAsync();

        // Assert
        Assert.That(keys.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(keys.Value.PrimaryKey, Is.Not.Null.And.Not.Empty);
            Assert.That(keys.Value.SecondaryKey, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public async Task Redis_WhenKeyRegenerated_ReturnsNewKey()
    {
        // Arrange
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string cacheName = "test-redis-regen";

        var createResult = await resourceGroup.Value.GetAllRedis()
            .CreateOrUpdateAsync(WaitUntil.Completed, cacheName, MinimalRedisContent());

        var originalKeys = await createResult.Value.GetKeysAsync();
        var originalPrimaryKey = originalKeys.Value.PrimaryKey;

        // Act
        var regenResult = await createResult.Value.RegenerateKeyAsync(
            new RedisRegenerateKeyContent(RedisRegenerateKeyType.Primary));

        // Assert
        Assert.That(regenResult.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(regenResult.Value.PrimaryKey, Is.Not.Null.And.Not.Empty);
            Assert.That(regenResult.Value.PrimaryKey, Is.Not.EqualTo(originalPrimaryKey));
            Assert.That(regenResult.Value.SecondaryKey, Is.EqualTo(originalKeys.Value.SecondaryKey));
        }
    }
}
