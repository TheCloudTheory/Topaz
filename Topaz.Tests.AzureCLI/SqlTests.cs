namespace Topaz.Tests.AzureCLI;

public class SqlTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-sql";

    [Test]
    public async Task SqlTests_WhenSqlServerIsDeployedViaTemplate_DeploymentShouldSucceed()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}");
        await RunAzureCliCommand(
            $"az deployment group create --name sql-server-deployment -g {ResourceGroup} --template-file \"/templates/sql-server-deployment.json\"",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("sql-server-deployment"));
                    Assert.That(
                        response["properties"]!["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded").IgnoreCase);
                });
            });
        await RunAzureCliCommand($"az group delete -n {ResourceGroup} --yes");
    }
}
