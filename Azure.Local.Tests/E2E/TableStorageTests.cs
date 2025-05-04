using Azure.Data.Tables;
using Azure.Local.Service.Storage;
using Azure.Local.Shared;

namespace Azure.Local.Tests.E2E
{
    public class TableStorageTests
    {
        private const string ConnectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://table.localhost:8899;QueueEndpoint=http://localhost:8899;TableEndpoint=http://localhost:8899/storage/devstoreaccount1;";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task TableStorageTests_WhenTableIsCreatedAndNoOtherTableIsPresent_ItShouldReturnOnlyNewTable()
        {
            // Arrange
            await Program.Main([
                "storage",
                "create",
                "--name",
                "devstoreaccount1"
            ]);

            await Program.Main([
                "start"
            ]);

            await Task.Delay(1000);

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
        public async Task TableStorageTests_WhenTableDoesNotExist_ItShouldBeCreatedAndThenDeleted()
        {
            // Arrange
            await Program.Main([
                "storage",
                "create",
                "--name",
                "devstoreaccount1"
            ]);

            await Program.Main([
                "start"
            ]);

            await Task.Delay(1000);

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
        }
    }
}