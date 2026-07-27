namespace Topaz.Tests.Terraform.AzureRm;

public class CosmosDbTests : AzureRmBatchFixture
{
    [Test]
    public void CosmosDbAccount_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("cosmos_account_name"), Is.EqualTo("tf-rm-cosmos"));
    }

    [Test]
    public void CosmosDbAccount_Endpoint_IsCorrect()
    {
        Assert.That(GetOutput<string>("cosmos_account_endpoint"),
            Does.Contain("tf-rm-cosmos.documents.topaz.local.dev"));
    }

    [Test]
    public void CosmosDbSqlDatabase_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("cosmos_sql_db_name"), Is.EqualTo("tf-rm-cosmos-db"));
    }

    [Test]
    public void CosmosDbSqlContainer_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("cosmos_sql_container_name"), Is.EqualTo("tf-rm-cosmos-container"));
    }

    [Test]
    public void CosmosDbSqlContainer_PartitionKey_IsCorrect()
    {
        Assert.That(GetOutput<string>("cosmos_sql_container_pk"), Is.EqualTo("/pk"));
    }

    [Test]
    public void CosmosDbSqlContainer_Throughput_IsCorrect()
    {
        Assert.That(GetOutput<int>("cosmos_sql_container_throughput"), Is.EqualTo(400));
    }
}
