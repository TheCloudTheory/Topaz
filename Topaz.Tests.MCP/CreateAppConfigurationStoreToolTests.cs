using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.AppConfiguration;
using NUnit.Framework;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateAppConfigurationStoreToolTests
{
    private const string StoreName = "mcp-appconfig-store";

    [OneTimeSetUp]
    public async Task DeleteLeftoverStore()
    {
        await Program.RunAsync([
            "appconfig", "delete",
            "--name", StoreName,
            "-g", McpTestFixture.ResourceGroupName,
            "--subscription-id", McpTestFixture.SubscriptionId.ToString()
        ]);
    }

    [Test, Order(1)]
    public async Task CreateAppConfigurationStore_ReturnsStoreName()
    {
        var result = await CreateAppConfigurationStoreTool.CreateAppConfigurationStore(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            StoreName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(StoreName));
    }

    [Test, Order(1)]
    public async Task CreateAppConfigurationStore_ReturnsEndpoint()
    {
        var result = await CreateAppConfigurationStoreTool.CreateAppConfigurationStore(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            StoreName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.That(result.Endpoint,
            Does.StartWith($"https://{StoreName}.{GlobalSettings.AppConfigurationDnsSuffix}:{GlobalSettings.DefaultAppConfigurationPort}/"));
    }

    [Test, Order(1)]
    public async Task CreateAppConfigurationStore_ReturnsConnectionString()
    {
        var result = await CreateAppConfigurationStoreTool.CreateAppConfigurationStore(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            StoreName,
            "eastus",
            McpTestFixture.ObjectId);

        Assert.Multiple(() =>
        {
            Assert.That(result.PrimaryReadWriteConnectionString, Does.Contain("Endpoint="));
            Assert.That(result.PrimaryReadWriteConnectionString, Does.Contain("Id="));
            Assert.That(result.PrimaryReadWriteConnectionString, Does.Contain("Secret="));
            Assert.That(result.PrimaryReadWriteConnectionString, Does.Contain(StoreName));
        });
    }

    [Test, Order(1)]
    public async Task CreateAppConfigurationStore_StoreExistsViaArmSdk()
    {
        await CreateAppConfigurationStoreTool.CreateAppConfigurationStore(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            StoreName,
            "eastus",
            McpTestFixture.ObjectId);

        var credential = new AzureLocalCredential(McpTestFixture.ObjectId);
        var armClient = new ArmClient(credential, McpTestFixture.SubscriptionId.ToString(), McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var store = await resourceGroup.Value.GetAppConfigurationStores().GetAsync(StoreName);

        Assert.That(store.Value.Data.Name, Is.EqualTo(StoreName));
    }
}
