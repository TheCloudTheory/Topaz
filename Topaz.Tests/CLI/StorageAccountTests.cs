namespace Topaz.Tests.CLI
{
    public class StorageAccountTests
    {
        [SetUp]
        public async Task SetUp()
        {
            await Program.Main(
            [
                "subscription",
                "delete",
                "--id",
                Guid.Empty.ToString()
            ]);

            await Program.Main(
            [
                "subscription",
                "create",
                "--id",
                Guid.Empty.ToString(),
                "--name",
                "sub-test"
            ]);

            await Program.Main([
                "group",
                "delete",
                "--name",
                "test"
            ]);

            await Program.Main([
                "group",
                "create",
                "--name",
                "test",
                "--location",
                "westeurope",
                "--subscriptionId",
                Guid.Empty.ToString()
            ]);

            await Program.Main([
                "storage",
                "account",
                "delete",
                "--name",
                "test"
            ]);

            await Program.Main([
                "storage",
                "account",
                "create",
                "--name",
                "test",
                "-g",
                "test",
                "--location",
                "westeurope",
                "--subscriptionId",
                Guid.Empty.ToString()
            ]);
        }

        [Test]
        public void StorageAccountTests_WhenNewStorageAccountIsRequested_ItShouldBeCreated()
        {
            var accountDirectoryPath = Path.Combine(".topaz", ".azure-storage", "test", "metadata.json");

            Assert.That(File.Exists(accountDirectoryPath), Is.True);
        }

        [Test]
        public async Task StorageAccountTests_WhenExistingStorageAccountIsDeleted_ItShouldBeDeleted()
        {
            var accountDirectoryPath = Path.Combine(".topaz", ".azure-storage", "test", "metadata.json");

            await Program.Main([
                "storage",
                "account",
                "delete",
                "--name",
                "test"
            ]);

            Assert.That(File.Exists(accountDirectoryPath), Is.False);
        }
    }
}