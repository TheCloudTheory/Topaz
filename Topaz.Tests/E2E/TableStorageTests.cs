using Topaz.CLI;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Storage;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.E2E
{
    public class TableStorageTests
    {
        private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
        private static readonly Guid SubscriptionId = Guid.Parse("8D37D5AF-468B-4F61-A664-9E8B5AE3E4C2");
    
        private const string SubscriptionName = "sub-test";
        private const string ResourceGroupName = "test";
        private const string StorageAccountName = "tablestoragetests";

        private string _key = null!;
        
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
                "-g",
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
        
            var credential = new AzureLocalCredential(Globals.GlobalAdminId);
            var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
            var subscription = await armClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
            var storageAccount = await resourceGroup.Value.GetStorageAccountAsync(StorageAccountName);
            var keys = storageAccount.Value.GetKeys().ToArray();

            _key = keys[0].Value;
        }
        
        [Test]
        public void TableStorageTests_WhenTableIsCreatedAndNoOtherTableIsPresent_ItShouldReturnOnlyNewTable()
        {
            // Arrange
            var tableClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

            // Act
            tableClient.CreateTable("testtablentoexisting");

            // Assert
            var tables = tableClient.Query().ToArray();
            var table = tables.First();

            Assert.That(table.Name, Is.EqualTo("testtablentoexisting"));

            tableClient.DeleteTable(table.Name);
            TableItem[] existingTables = [.. tableClient.Query()];

            Assert.That(existingTables.SingleOrDefault(existingTable => existingTable.Name == "testtablentoexisting"), Is.Null);
        }

        [Test]
        public void TableStorageTests_WhenEntityIsInserted_ItShouldBeAvailableOverEmulator()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            
            // Act
            var properties = tableServiceClient.GetProperties();
            
            // Assert
            Assert.That(properties, Is.Not.Null);
        }

        [Test]
        public void TableStorageTests_WhenTablePropertiesAreSet_UpdatedValuesMustBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            var existing = tableServiceClient.GetProperties().Value;
            var originalRead = existing.Logging.Read;

            existing.Logging.Read = !originalRead;

            // Act
            tableServiceClient.SetProperties(existing);

            // Assert
            var updated = tableServiceClient.GetProperties().Value;
            Assert.That(updated.Logging.Read, Is.EqualTo(!originalRead));
        }
        
        [Test]
        public void TableStorageTests_WhenTableACLsAreRequested_TheyMustBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
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

        [Test]
        public void TableStorageTests_WhenCreateTableIfNotExistsIsCalledMultipleTimes_ItShouldWork()
        {
            // Arrange
            const string tableName = "testtable";
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            
            // Act
            tableServiceClient.CreateTableIfNotExists(tableName);
            var tableClient = tableServiceClient.GetTableClient(tableName);
            tableClient.AddEntity(new TestEntity
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });
            
            tableServiceClient.CreateTableIfNotExists(tableName);
            tableClient = tableServiceClient.GetTableClient(tableName);
            var query = tableClient.Query<TestEntity>().ToArray();
            
            // Assert
            Assert.That(query, Has.Length.EqualTo(1));
        }

        [Test]
        public void TableStorageTest_WhenInvalidKeyIsProvidedInConnectionString_ItShouldFail()
        {
            // Arrange
            const string tableName = "testtable";
            var invalidKey = Convert.ToBase64String("invalid-key"u8.ToArray());
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, invalidKey));
            
            // Assert
            Assert.Throws<RequestFailedException>(() => tableServiceClient.CreateTableIfNotExists(tableName));
        }

        [Test]
        public void TableStorageTests_WhenEntityIsDeleted_ItShouldNoLongerBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            var entity = tableClient.Query<TestEntity>().First();

            // Act
            tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey, ETag.All);

            // Assert
            var entities = tableClient.Query<TestEntity>().ToArray();
            Assert.That(entities, Is.Empty);
        }

        [Test]
        public void TableStorageTests_WhenEntityIsDeletedWithMatchingETag_ItShouldSucceed()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            var entity = tableClient.Query<TestEntity>().First();

            // Act
            tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey, entity.ETag);

            // Assert
            var entities = tableClient.Query<TestEntity>().ToArray();
            Assert.That(entities, Is.Empty);
        }

        [Test]
        public void TableStorageTests_WhenEntityIsDeletedWithStalETag_ItShouldThrow()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            var entity = tableClient.Query<TestEntity>().First();
            // Update to create a newer ETag
            entity.Name = "bar";
            tableClient.UpdateEntity(entity, entity.ETag);

            // Act & Assert — deleting with the original (now stale) ETag must fail
            Assert.Throws<RequestFailedException>(() =>
                tableClient.DeleteEntity(entity.PartitionKey, entity.RowKey, entity.ETag));
        }

        [Test]
        public void TableStorageTests_WhenEntityIsInsertedAndFetchedByKey_ItMustReturnCorrectEntity()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            tableClient.AddEntity(new TestEntity
            {
                PartitionKey = "test",
                RowKey = "1",
                Name = "foo"
            });

            // Act
            var entity = tableClient.GetEntity<TestEntity>("test", "1");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(entity.Value.PartitionKey, Is.EqualTo("test"));
                Assert.That(entity.Value.RowKey, Is.EqualTo("1"));
                Assert.That(entity.Value.Name, Is.EqualTo("foo"));
            });
        }

        [Test]
        public void TableStorageTests_WhenNonExistentEntityIsFetchedByKey_ItShouldThrow()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Act & Assert
            Assert.Throws<RequestFailedException>(() => tableClient.GetEntity<TestEntity>("test", "nonexistent"));
        }

        [Test]
        public void TableStorageTests_WhenNonExistentEntityIsDeleted_ItShouldThrow()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            tableServiceClient.CreateTable("testtable");
            var tableClient = tableServiceClient.GetTableClient("testtable");

            // Act & Assert
            Assert.Throws<RequestFailedException>(() =>
                tableClient.DeleteEntity("test", "nonexistent", ETag.All));
        }

        [Test]
        public void TableStorageTests_WhenServiceStatsAreRequested_TheyMustBeReturned()
        {
            // Arrange
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));

            // Act
            var stats = tableServiceClient.GetStatistics();

            // Assert
            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.Value, Is.Not.Null);
            Assert.That(stats.Value.GeoReplication, Is.Not.Null);
            Assert.That(stats.Value.GeoReplication.Status.ToString(), Is.EqualTo("live"));
            Assert.That(stats.Value.GeoReplication.LastSyncedOn, Is.Not.Null);
        }

        [Test]
        public async Task TableStorageTests_WhenPreflightRequestMatchesCorsRule_ItShouldReturn200WithCorsHeaders()
        {
            // Arrange — configure a CORS rule on the table service
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            var properties = tableServiceClient.GetProperties().Value;
            properties.CorsRules.Clear();
            properties.CorsRules.Add(new TableCorsRule(
                allowedOrigins: "http://cors-test.example.com",
                allowedMethods: "GET,POST",
                allowedHeaders: "*",
                exposedHeaders: "x-ms-request-id",
                maxAgeInSeconds: 300));
            tableServiceClient.SetProperties(properties);

            var baseUrl = $"http://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}";
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Options, $"{baseUrl}/Tables");
            request.Headers.Add("Origin", "http://cors-test.example.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await httpClient.SendAsync(request);

            // Assert
            Assert.That((int)response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin").First(),
                Is.EqualTo("http://cors-test.example.com"));
            Assert.That(response.Headers.Contains("Access-Control-Allow-Methods"), Is.True);
        }

        [Test]
        public async Task TableStorageTests_WhenPreflightRequestOriginDoesNotMatchAnyCorsRule_ItShouldReturn403()
        {
            // Arrange — configure a CORS rule that does not match the request origin
            var tableServiceClient = new TableServiceClient(TopazResourceHelpers.GetAzureStorageConnectionString(StorageAccountName, _key));
            var properties = tableServiceClient.GetProperties().Value;
            properties.CorsRules.Clear();
            properties.CorsRules.Add(new TableCorsRule(
                allowedOrigins: "http://allowed-origin.example.com",
                allowedMethods: "GET",
                allowedHeaders: "*",
                exposedHeaders: "",
                maxAgeInSeconds: 300));
            tableServiceClient.SetProperties(properties);

            var baseUrl = $"http://{StorageAccountName}.table.storage.topaz.local.dev:{GlobalSettings.DefaultTableStoragePort}";
            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Options, $"{baseUrl}/Tables");
            request.Headers.Add("Origin", "http://other-origin.example.com");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await httpClient.SendAsync(request);

            // Assert
            Assert.That((int)response.StatusCode, Is.EqualTo(403));
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