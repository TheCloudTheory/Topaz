using Azure.ResourceManager.Storage;
using Xunit;

namespace Topaz.Example.BicepModuleTesting;

[Collection("Topaz")]
public class StorageModuleTests
{
    private readonly TopazFixture _topaz;

    public StorageModuleTests(TopazFixture topaz) => _topaz = topaz;

    [Theory]
    [InlineData("stmodtest001", "Standard_LRS")]
    [InlineData("stmodtest002", "Standard_GRS")]
    [InlineData("stmodtest003", "Premium_LRS")]
    public async Task Deploy_StorageModule_ShouldHaveCorrectSku(
        string accountName, string expectedSku)
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "storage.bicep",
            $"storageAccountName={accountName} sku={expectedSku}");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync(accountName)).Value;

        Assert.Equal(expectedSku, account.Data.Sku.Name.ToString());
        Assert.Equal("StorageV2", account.Data.Kind.ToString());
    }

    [Fact]
    public async Task Deploy_StorageModule_WithTags_ShouldPreserveTags()
    {
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "storage.bicep",
            "storageAccountName=sttagtest " +
            "tags={\"environment\":\"test\"} " +
            "tags={\"owner\":\"platform-team\"}");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync("sttagtest")).Value;

        Assert.Equal("test", account.Data.Tags["environment"]);
        Assert.Equal("platform-team", account.Data.Tags["owner"]);
    }

    [Fact]
    public async Task Deploy_StorageModule_DefaultSku_ShouldBeStandardLrs()
    {
        // Regression test for the module contract — verifies the default has not silently changed
        BicepDeployer.Deploy(
            "rg-bicep-module-tests",
            "storage.bicep",
            "storageAccountName=stdefaultsku");

        var account = (await _topaz.ResourceGroup.GetStorageAccountAsync("stdefaultsku")).Value;

        Assert.Equal("Standard_LRS", account.Data.Sku.Name.ToString());
    }
}
