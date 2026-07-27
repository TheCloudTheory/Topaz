using System.Text.Json;
using Topaz.CLI;
using Topaz.Service.Storage.Models;
using Topaz.Shared;

namespace Topaz.Tests.CLI
{
    public class StorageAccountTests
    {
        private static readonly Guid SubscriptionId = Guid.NewGuid();
    
        private const string SubscriptionName = "sub-test";
        private const string ResourceGroupName = "test";
        private const string StorageAccountName = "testsa";
        private const string StorageAccountHnsName = "testsahns";
        
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
                "storage",
                "account",
                "delete",
                "--name",
                StorageAccountName,
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
                StorageAccountName,
                "-g",
                ResourceGroupName,
                "--location",
                "westeurope",
                "--subscription-id",
                SubscriptionId.ToString()
            ]);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await Program.RunAsync([
                "storage",
                "account",
                "delete",
                "--name",
                StorageAccountHnsName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString()
            ]);
        }

        [Test]
        public Task StorageAccountTests_WhenNewStorageAccountIsRequested_ItShouldBeCreated()
        {
            try
            {
                var accountDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                    SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", StorageAccountName, "metadata.json");

                Assert.That(File.Exists(accountDirectoryPath), Is.True);
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }
        
        [Test]
        public async Task StorageAccountTests_WhenNewStorageAccountIsRequestedWithHns_HnsShouldBeEnabled()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "create",
                "--name",
                StorageAccountHnsName,
                "-g",
                ResourceGroupName,
                "--location",
                "westeurope",
                "--subscription-id",
                SubscriptionId.ToString(),
                "--enable-hierarchical-namespace"
            ]);
            
            Assert.That(result, Is.Zero);
            
            var accountDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", StorageAccountHnsName, "metadata.json");

            Assert.That(File.Exists(accountDirectoryPath), Is.True);
            
            var json = await File.ReadAllTextAsync(accountDirectoryPath);
            var resource = JsonSerializer.Deserialize<StorageAccountResource>(json, GlobalSettings.JsonOptions);
            
            Assert.That(resource, Is.Not.Null);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(resource.Properties.IsHnsEnabled, Is.True);
                Assert.That(resource.Kind, Is.EqualTo("StorageV2"));
            }
        }

        [Test]
        public async Task StorageAccountTests_WhenExistingStorageAccountIsDeleted_ItShouldBeDeleted()
        {
            var accountDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", StorageAccountName, "metadata.json");

            await Program.RunAsync([
                "storage",
                "account",
                "delete",
                "--name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString()
            ]);

            Assert.That(File.Exists(accountDirectoryPath), Is.False);
        }

        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToListItsKeys()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "keys",
                "list",
                "--account-name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString()
            ]);
            
            Assert.That(result, Is.Zero);
        }
        
        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToGetConnectionString()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "show-connection-string",
                "--name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString()
            ]);
            
            Assert.That(result, Is.Zero);
        }

        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToRegenerateKey()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "keys",
                "renew",
                "--account-name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString(),
                "--key-name",
                "key1"
            ]);

            Assert.That(result, Is.Zero);
        }

        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToGenerateAccountSas()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "generate-sas",
                "--account-name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString(),
                "--services",
                "b",
                "--resource-types",
                "s",
                "--permissions",
                "r",
                "--expiry",
                DateTimeOffset.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
            ]);

            Assert.That(result, Is.Zero);
        }

        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToGenerateServiceSas()
        {
            var result = await Program.RunAsync([
                "storage",
                "account",
                "generate-service-sas",
                "--account-name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName,
                "--subscription-id",
                SubscriptionId.ToString(),
                "--canonicalized-resource",
                $"/blob/{StorageAccountName}/mycontainer",
                "--resource",
                "c",
                "--permissions",
                "r",
                "--expiry",
                DateTimeOffset.UtcNow.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
            ]);

            Assert.That(result, Is.Zero);
        }
    }
}