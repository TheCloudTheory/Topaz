using Azure.Data.Tables;

namespace Azure.Local.Tests.E2E
{
    public class TableStorageTests
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://table.localhost:8899;QueueEndpoint=http://localhost:8899;TableEndpoint=http://localhost:8899/storage/devstoreaccount1;";

        [SetUp]
        public async Task SetUp()
        {
            await Program.Main([
                "storage",
                "delete",
                "--name",
                "devstoreaccount1"
            ]);

            await Program.Main([
                "storage",
                "create",
                "--name",
                "devstoreaccount1"
            ]);
        }

        [TearDown]
        public async Task TearDown()
        {
            await Program.Main([
                "storage",
                "delete",
                "--name",
                "devstoreaccount1"
            ]);
        }

        [Test]
        public void TableStorageTests_WhenTableIsCreatedAndNoOtherTableIsPresent_ItShouldReturnOnlyNewTable()
        {
            // Arrange
            var tableClient = new TableServiceClient(ConnectionString);
            var existingTables = tableClient.Query().ToArray();

            foreach(var table in existingTables)
            {
                tableClient.DeleteTable(table.Name);
            }

            // Act           
            tableClient.CreateTableIfNotExists("testtable");

            // Assert
            var tables = tableClient.Query().ToArray();

            Assert.That(tables, Has.Length.EqualTo(1));
        }

        [Test]
        public void TableStorageTests_WhenTableDoesNotExist_ItShouldBeCreatedAndThenDeleted()
        {
            // Arrange
            var tableClient = new TableServiceClient(ConnectionString);
            var existingTables = tableClient.Query().ToArray();

            // Act
            tableClient.CreateTable("testtable");

            // Assert
            Assert.That(existingTables, Is.Empty);

            var tables = tableClient.Query().ToArray();

            Assert.That(tables, Has.Length.EqualTo(1));

            var table = tables.First();

            Assert.That(table.Name, Is.EqualTo("testtable"));

            tableClient.DeleteTable(table.Name);
            existingTables = [.. tableClient.Query()];

            Assert.That(existingTables, Is.Empty);
        }
    }
}