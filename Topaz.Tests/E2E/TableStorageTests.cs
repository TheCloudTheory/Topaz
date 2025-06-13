using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E
{
    public class TableStorageTests
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
                "storage",
                "account",
                "delete",
                "--name",
                "devstoreaccount1"
            ]);

            await Program.Main([
                "group",
                "delete",
                "--name",
                "rg-test"
            ]);

            await Program.Main([
                "group",
                "create",
                "--name",
                "rg-test",
                "--location",
                "westeurope",
                "--subscriptionId",
                Guid.Empty.ToString()
            ]);

            await Program.Main([
                "storage",
                "account",
                "create",
                "--name",
                "devstoreaccount1",
                "-g",
                "rg-test",
                "--location",
                "westeurope",
                "--subscriptionId",
                Guid.Empty.ToString()
            ]);
        }
        
        [Test]
        public void TableStorageTests_WhenTableIsCreatedAndNoOtherTableIsPresent_ItShouldReturnOnlyNewTable()
        {
            // Arrange
            var tableClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var tableClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var entities = tableClient.Query<TestEntity>("PartitionKey eq 'test'").OrderBy(e => e.RowKey).ToArray();

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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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

        [Test]
        public void TableStorageTests_WhenEntityIsUpdatedWithETag_ItShouldBeAvailableOverEmulator()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo",
            });

            var entity = tableClient.Query<TestEntity>().First();

            // Act
            entity.Name = "bar";
            tableClient.UpdateEntity(entity, entity.ETag);

            // Assert
            var updatedEntity = tableClient.Query<TestEntity>().ToArray().First();
            
            Assert.Multiple(() =>
            {
                Assert.That(updatedEntity.Name, Is.EqualTo("bar"));
                Assert.That(updatedEntity.PartitionKey, Is.EqualTo("test"));
                Assert.That(updatedEntity.RowKey, Is.EqualTo("1"));
            });
        }

        [Test]
        public void TableStorageTests_WhenEntityIsInsertedAndFetched_ItMustContainETagAndTimestamp()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo",
            });

            // Act
            var entity = tableClient.Query<TestEntity>().First();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(entity.ETag.ToString(), Is.Not.EqualTo(""));
                Assert.That(entity.ETag.ToString(), Is.Not.EqualTo("{}"));
                Assert.That(entity.Timestamp, Is.Not.Null);
            });
        }

        [Test]
        public void TableStorageTests_WhenEntityIsUpdatedConcurrently_ItsETagMustBeRespected()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo",
            });

            var entity = tableClient.Query<TestEntity>().First();
            var sameEntity = tableClient.Query<TestEntity>().First();

            // Act
            entity.Name = "bar";
            sameEntity.Name = "foobar";
            tableClient.UpdateEntity(entity, entity.ETag);

            // Assert
            Assert.Throws<RequestFailedException>(() => {
                tableClient.UpdateEntity(sameEntity, sameEntity.ETag);
            });
        }

        [Test]
        public void TableStorageTests_WhenEntityIsUpdatedConcurrentlyButIsRequestedToBeUpdatedUnconditionally_ItShouldBeUpdated()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity()
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo",
            });

            var entity = tableClient.Query<TestEntity>().First();
            var sameEntity = tableClient.Query<TestEntity>().First();

            // Act
            entity.Name = "bar";
            sameEntity.Name = "foobar";
            tableClient.UpdateEntity(entity, entity.ETag);
            tableClient.UpdateEntity(sameEntity, ETag.All);

            var updatedEntity = tableClient.Query<TestEntity>().ToArray().First();
            
            Assert.Multiple(() =>
            {
                Assert.That(updatedEntity.Name, Is.EqualTo("foobar"));
                Assert.That(updatedEntity.PartitionKey, Is.EqualTo("test"));
                Assert.That(updatedEntity.RowKey, Is.EqualTo("1"));
            });
        }

        [Test]
        public void TableStorageTests_WhenMergeOperationIsPerformed_ItMustBeSupported()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            tableClient.UpdateEntity(entity, ETag.All, TableUpdateMode.Merge);

            // Assert
            var updatedEntity = tableClient.Query<TestEntity>().ToArray().First();

            Assert.Multiple(() =>
            {
                Assert.That(updatedEntity.Name, Is.EqualTo("bar"));
                Assert.That(updatedEntity.PartitionKey, Is.EqualTo("test"));
                Assert.That(updatedEntity.RowKey, Is.EqualTo("1"));
            });
        }

        [Test]
        public void TableStorageTests_WhenReplaceOperationIsPerformed_ItMustBeSupported()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
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
            tableClient.UpdateEntity(entity, ETag.All, TableUpdateMode.Replace);

            // Assert
            var updatedEntity = tableClient.Query<TestEntity>().ToArray().First();

            Assert.Multiple(() =>
            {
                Assert.That(updatedEntity.Name, Is.EqualTo("bar"));
                Assert.That(updatedEntity.PartitionKey, Is.EqualTo("test"));
                Assert.That(updatedEntity.RowKey, Is.EqualTo("1"));
            });
        }

        [Test]
        public void TableStorageTests_WhenTablePropertiesAreRequested_TheyMustBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            
            // Act
            var properties = tableServiceClient.GetProperties();
            
            // Assert
            Assert.That(properties, Is.Not.Null);
        }
        
        [Test]
        public void TableStorageTests_WhenTableACLsAreRequested_TheyMustBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString("devstoreaccount1"));
            tableServiceClient.CreateTable("testtable");

            var tableClient = tableServiceClient.GetTableClient("testtable");
            
            // Act
            tableClient.SetAccessPolicy([
                new TableSignedIdentifier("some_id",
                    new TableAccessPolicy(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(1), "raud"))
            ]);
            var acls = tableClient.GetAccessPolicies();
            
            // Assert
            Assert.That(acls, Is.Not.Null);
            Assert.That(acls.Value, Has.Count.EqualTo(1));
            Assert.That(acls.Value[0].Id, Is.EqualTo("some_id"));
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