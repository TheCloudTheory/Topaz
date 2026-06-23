using Azure.ResourceManager.CosmosDB;
using Xunit;

namespace Topaz.Example.BicepModuleTesting;

[Collection("Topaz")]
public class CosmosDbModuleTests
{
    private readonly TopazFixture _topaz;

    public CosmosDbModuleTests(TopazFixture topaz) => _topaz = topaz;

    [Fact]
    public async Task Deploy_CosmosModule_ShouldProvisionGlobalDocumentDb()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "cosmos.bicep",
            "accountName=cosmos-mod-test");

        var accounts = _topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
        var account = accounts.FirstOrDefault(a => a.Data.Name == "cosmos-mod-test");

        Assert.NotNull(account);
        Assert.Equal("GlobalDocumentDB", account.Data.Kind.ToString());
    }

    [Fact]
    public async Task Deploy_CosmosModule_WithTags_ShouldPreserveTags()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "cosmos.bicep",
            "accountName=cosmos-tagged " +
            "tags.environment=test");

        var accounts = _topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
        var account = accounts.First(a => a.Data.Name == "cosmos-tagged");

        Assert.Equal("test", account.Data.Tags["environment"]);
    }

    [Fact]
    public async Task Deploy_CosmosModule_TwoRegions_BothShouldAppearInLocations()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "cosmos.bicep",
            "accountName=cosmos-multi-region " +
            "secondaryLocation=northeurope");

        var accounts = _topaz.ResourceGroup.GetCosmosDBAccounts().ToList();
        var account = accounts.First(a => a.Data.Name == "cosmos-multi-region");

        Assert.Contains(account.Data.Locations, l => l.LocationName == "westeurope");
        Assert.Contains(account.Data.Locations, l => l.LocationName == "northeurope");
    }
}
