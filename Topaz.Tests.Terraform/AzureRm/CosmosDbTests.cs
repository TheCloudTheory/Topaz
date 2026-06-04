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
}
