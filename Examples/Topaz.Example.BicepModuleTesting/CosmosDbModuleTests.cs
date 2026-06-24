using Azure.ResourceManager.CosmosDB;
using Xunit;

namespace Topaz.Example.BicepModuleTesting;

[Collection("Topaz")]
public class CosmosDbModuleTests(TopazFixture topaz)
{
    [Fact]
    public Task Deploy_CosmosModule_ShouldProvisionGlobalDocumentDb()
    {
        try
        {
            BicepDeployer.Deploy(
                "rg-bicep-module-tests",
                "cosmos.bicep",
                "accountName=cosmos-mod-test");

            var accounts = topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
            var account = accounts.FirstOrDefault(a => a.Data.Name == "cosmos-mod-test");

            Assert.NotNull(account);
            Assert.Equal("GlobalDocumentDB", account.Data.Kind.ToString());
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Fact]
    public Task Deploy_CosmosModule_WithTags_ShouldPreserveTags()
    {
        try
        {
            BicepDeployer.Deploy(
                "rg-bicep-module-tests",
                "cosmos.bicep",
                "accountName=cosmos-tagged " +
                "tags.environment=test");

            var accounts = topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
            var account = accounts.First(a => a.Data.Name == "cosmos-tagged");

            Assert.Equal("test", account.Data.Tags["environment"]);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    [Fact]
    public Task Deploy_CosmosModule_TwoRegions_BothShouldAppearInLocations()
    {
        try
        {
            BicepDeployer.Deploy(
                "rg-bicep-module-tests",
                "cosmos.bicep",
                "accountName=cosmos-multi-region " +
                "secondaryLocation=northeurope");

            var accounts = topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
            var account = accounts.First(a => a.Data.Name == "cosmos-multi-region");

            Assert.Contains(account.Data.Locations, l => l.LocationName == "westeurope");
            Assert.Contains(account.Data.Locations, l => l.LocationName == "northeurope");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
