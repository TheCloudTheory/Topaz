using Topaz.CLI;

namespace Topaz.Tests.CLI
{
    public class StorageAccountTests
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

        [Test]
        public void StorageAccountTests_WhenNewStorageAccountIsRequested_ItShouldBeCreated()
        {
            var accountDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
                SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".azure-storage", StorageAccountName, "metadata.json");

            Assert.That(File.Exists(accountDirectoryPath), Is.True);
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
            
            Assert.That(result, Is.EqualTo(0));
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
            
            Assert.That(result, Is.EqualTo(0));
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

            Assert.That(result, Is.EqualTo(0));
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

            Assert.That(result, Is.EqualTo(0));
        }
    }
}