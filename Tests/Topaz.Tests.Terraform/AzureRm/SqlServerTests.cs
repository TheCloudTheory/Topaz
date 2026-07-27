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

    [Test]
    public void SqlDatabase_CreateAndDestroy_Succeeds()
    {
        Assert.That(GetOutput<string>("sql_db_name"), Is.EqualTo("tf-rm-sql-db"));
    }

    [Test]
    public void SqlDatabase_Collation_IsDefault()
    {
        Assert.That(GetOutput<string>("sql_db_collation"),
            Is.EqualTo("SQL_Latin1_General_CP1_CI_AS").IgnoreCase);
    }
}
