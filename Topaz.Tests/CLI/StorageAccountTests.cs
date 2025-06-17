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
            await Program.Main(
            [
                "subscription",
                "delete",
                "--id",
                SubscriptionId.ToString()
            ]);

            await Program.Main(
            [
                "subscription",
                "create",
                "--id",
                SubscriptionId.ToString(),
                "--name",
                SubscriptionName
            ]);

            await Program.Main([
                "group",
                "delete",
                "--name",
                ResourceGroupName
            ]);

            await Program.Main([
                "group",
                "create",
                "--name",
                ResourceGroupName,
                "--location",
                "westeurope",
                "--subscription-id",
                SubscriptionId.ToString()
            ]);

            await Program.Main([
                "storage",
                "account",
                "delete",
                "--name",
                StorageAccountName
            ]);

            await Program.Main([
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
            var accountDirectoryPath = Path.Combine(".topaz", ".azure-storage", StorageAccountName, "metadata.json");

            Assert.That(File.Exists(accountDirectoryPath), Is.True);
        }

        [Test]
        public async Task StorageAccountTests_WhenExistingStorageAccountIsDeleted_ItShouldBeDeleted()
        {
            var accountDirectoryPath = Path.Combine(".topaz", ".azure-storage", StorageAccountName, "metadata.json");

            await Program.Main([
                "storage",
                "account",
                "delete",
                "--name",
                StorageAccountName
            ]);

            Assert.That(File.Exists(accountDirectoryPath), Is.False);
        }

        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToListItsKeys()
        {
            var result = await Program.Main([
                "storage",
                "account",
                "keys",
                "list",
                "--account-name",
                StorageAccountName,
                "--resource-group",
                ResourceGroupName
            ]);
            
            Assert.That(result, Is.EqualTo(0));
        }
        
        [Test]
        public async Task StorageAccountTests_WhenStorageAccountExists_ItShouldBePossibleToGetConnectionString()
        {
            var result = await Program.Main([
                "storage",
                "account",
                "show-connection-string",
                "--name",
                StorageAccountName,
            ]);
            
            Assert.That(result, Is.EqualTo(0));
        }
    }
}