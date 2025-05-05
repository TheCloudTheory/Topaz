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

        [Test]
        public void TableStorageTests_WhenEntityIsInserted_ItShouldBeAvailableOverEmulator()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(ConnectionString);
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Act
            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            // Assert
            var entities = tableClient.Query<TestEntity>("PartitionKey eq 'test'").ToArray();

            Assert.That(entities, Has.Length.EqualTo(1));
        }

        [Test]
        public void TableStorageTests_WhenMultipleEntitiesAreInserted_TheyShouldBeAvailableOverEmulator()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(ConnectionString);
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Act
            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "2",
                Name = "foo"
            });

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "3",
                Name = "foo"
            });

            // Assert
            var entities = tableClient.Query<TestEntity>("PartitionKey eq 'test'").ToArray();

            Assert.That(entities, Has.Length.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(entities[0].RowKey, Is.EqualTo("1"));
                Assert.That(entities[1].RowKey, Is.EqualTo("2"));
                Assert.That(entities[2].RowKey, Is.EqualTo("3"));
            });
        }

        [Test]
        public void TableStorageTests_WhenDuplicatedEntityIsInserted_ItShouldNotBeAdded()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(ConnectionString);
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Act
            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            Assert.Throws<RequestFailedException>(() => tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            }));
        }

        [Test]
        public void TableStorageTests_WhenTableDoesNotExist_ErrorShouldBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(ConnectionString);
            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Assert
            Assert.Throws<RequestFailedException>(() => tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            }));
        }

        [Test]
        public void TableStorageTests_WhenEntityIsUpdated_ItShouldBeAvailableOverEmulator()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(ConnectionString);
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            var entity = tableClient.Query<TestEntity>().First();

            // Act
            entity.Name = "bar";
            tableClient.UpdateEntity(entity, ETag.All);

            // Assert
            var updatedEntity = tableClient.Query<TestEntity>().ToArray().First();
            
            Assert.Multiple(() =>
            {
                Assert.That(updatedEntity.Name, Is.EqualTo("bar"));
                Assert.That(updatedEntity.PartitionKey, Is.EqualTo("test"));
                Assert.That(updatedEntity.RowKey, Is.EqualTo("1"));
            });
        }

        private class TestEntity : ITableEntity
        {
            public string? Name { get; set; }
            public string? PartitionKey { get; set; }
            public string? RowKey { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
            public ETag ETag { get; set; }
        }
    }
}