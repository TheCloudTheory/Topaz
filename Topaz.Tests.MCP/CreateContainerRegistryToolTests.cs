using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateContainerRegistryToolTests
{
    private const string RegistryName = "mcptestregistry";

    [Test]
    public async Task CreateContainerRegistry_ReturnsRegistryName()
    {
        var result = await CreateContainerRegistryTool.CreateContainerRegistry(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            RegistryName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.RegistryName, Is.EqualTo(RegistryName));
    }

    [Test]
    public async Task CreateContainerRegistry_ReturnsLoginServer()
    {
        var result = await CreateContainerRegistryTool.CreateContainerRegistry(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            RegistryName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.LoginServer,
            Does.Contain($"{RegistryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}"));
    }

    [Test]
    public async Task CreateContainerRegistry_AdminEnabledReturnsCredentials()
    {
        var result = await CreateContainerRegistryTool.CreateContainerRegistry(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            RegistryName,
            "westeurope",
            McpTestFixture.ObjectId,
            adminUserEnabled: true);

        Assert.Multiple(() =>
        {
            Assert.That(result.AdminUsername, Is.Not.Null.And.Not.Empty);
            Assert.That(result.AdminPassword, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task CreateContainerRegistry_AdminDisabledReturnsNullCredentials()
    {
        var result = await CreateContainerRegistryTool.CreateContainerRegistry(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            RegistryName,
            "westeurope",
            McpTestFixture.ObjectId,
            adminUserEnabled: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.AdminUsername, Is.Null);
            Assert.That(result.AdminPassword, Is.Null);
        });
    }

    [Test]
    public async Task CreateContainerRegistry_RegistryExistsViaArmSdk()
    {
        await CreateContainerRegistryTool.CreateContainerRegistry(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            RegistryName,
            "westeurope",
            McpTestFixture.ObjectId);

        var armClient = new ArmClient(
            new AzureLocalCredential(McpTestFixture.ObjectId),
            McpTestFixture.SubscriptionId.ToString(),
            McpTestFixture.ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroupAsync(McpTestFixture.ResourceGroupName);
        var registry = await rg.Value.GetContainerRegistryAsync(RegistryName);

        Assert.That(registry.Value.Data.Name, Is.EqualTo(RegistryName));
    }
}
