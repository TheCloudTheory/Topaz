namespace Topaz.Tests.Terraform.AzureRm;

public class SqlServerTests : AzureRmBatchFixture
{
    [Test]
    public void SqlServer_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("sql_server_name"), Is.EqualTo("tf-rm-sql"));
    }

    [Test]
    public void SqlServer_FullyQualifiedDomainName_IsCorrect()
    {
        Assert.That(GetOutput<string>("sql_server_fqdn"),
            Is.EqualTo("tf-rm-sql.database.topaz.local.dev"));
    }
}
