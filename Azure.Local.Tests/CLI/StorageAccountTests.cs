namespace Azure.Local.Tests.CLI
{
    public class StorageAccountTests
    {
        [Test]
        public async Task StorageAccountTests_WhenNewStorageAccountIsRequested_ItShouldBeCreated()
        {
            var accountDirectoryPath = Path.Combine(".azure-storage", "test");

            await Program.Main([
                "storage",
                "delete",
                "--name",
                "test"
            ]);

            var result = await Program.Main([
                "storage",
                "create",
                "--name",
                "test"
            ]);
            
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(0));
                Assert.That(Directory.Exists(accountDirectoryPath), Is.True);
            });
        }

        [Test]
        public async Task StorageAccountTests_WhenExistingStorageAccountIsDeleted_ItShouldBeDeleted()
        {
            var accountDirectoryPath = Path.Combine(".azure-storage", "test");

            await Program.Main([
                "storage",
                "delete",
                "--name",
                "test"
            ]);

            var result = await Program.Main([
                "storage",
                "create",
                "--name",
                "test"
            ]);

            await Program.Main([
                "storage",
                "delete",
                "--name",
                "test"
            ]);
            
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(0));
                Assert.That(Directory.Exists(accountDirectoryPath), Is.False);
            });
        }
    }
}