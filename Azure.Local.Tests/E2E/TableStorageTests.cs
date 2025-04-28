using Azure.Data.Tables;

namespace Azure.Local.Tests.E2E
{
    public class TableStorageTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TableStorageTests_WhenTableIsCreatedAndNoOtherTableIsPresent_ItShouldReturnOnlyNewTable()
        {
            var x = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://table.localhost:8899;QueueEndpoint=http://localhost:8899;TableEndpoint=http://localhost:8899/storage/table;";
            // Arrange
            var tableClient = new TableServiceClient(x);

            // Act           
            tableClient.CreateTable("testtable");

            // Assert
            var tables = tableClient.Query();

            Assert.That(tables.Count(), Is.EqualTo(1));
        }
    }
}