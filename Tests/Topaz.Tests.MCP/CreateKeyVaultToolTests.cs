using Azure.Security.KeyVault.Secrets;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.MCP.Tools;
using Topaz.ResourceManager;
using Topaz.Shared;

namespace Topaz.Tests.MCP;

[TestFixture]
public class CreateKeyVaultToolTests
{
    private const string KeyVaultName = "kv-mcp-create-test";
    private const string KeyVaultWithSecretName = "kv-mcp-secret-test";

    [Test]
    public async Task CreateKeyVault_ReturnsVaultName()
    {
        var result = await CreateKeyVaultTool.CreateKeyVault(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            KeyVaultName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(KeyVaultName));
    }

    [Test]
    public async Task CreateKeyVault_ReturnsVaultUri()
    {
        var result = await CreateKeyVaultTool.CreateKeyVault(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            KeyVaultName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.VaultUri,
            Does.StartWith($"https://{KeyVaultName}.{GlobalSettings.KeyVaultDnsSuffix}:{GlobalSettings.DefaultKeyVaultPort}"));
    }

    [Test]
    public async Task CreateKeyVault_WithoutSecret_ReturnsNullSeededSecret()
    {
        var result = await CreateKeyVaultTool.CreateKeyVault(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            KeyVaultName,
            "westeurope",
            McpTestFixture.ObjectId);

        Assert.That(result.SeededSecret, Is.Null);
    }

    [Test]
    public async Task CreateKeyVault_WithSecret_ReturnsSecretName()
    {
        var result = await CreateKeyVaultTool.CreateKeyVault(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            KeyVaultWithSecretName,
            "westeurope",
            McpTestFixture.ObjectId,
            secretName: "my-secret",
            secretValue: "super-secret-value");

        Assert.That(result.SeededSecret, Is.EqualTo("my-secret"));
    }

    [Test]
    public async Task CreateKeyVault_WithSecret_SecretIsReadableFromVault()
    {
        await CreateKeyVaultTool.CreateKeyVault(
            McpTestFixture.SubscriptionId,
            McpTestFixture.ResourceGroupName,
            KeyVaultWithSecretName,
            "westeurope",
            McpTestFixture.ObjectId,
            secretName: "my-secret",
            secretValue: "super-secret-value");

        var secretClient = new SecretClient(
            vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(KeyVaultWithSecretName),
            credential: new AzureLocalCredential(McpTestFixture.ObjectId),
            new SecretClientOptions { DisableChallengeResourceVerification = true });

        var secret = await secretClient.GetSecretAsync("my-secret");

        Assert.That(secret.Value.Value, Is.EqualTo("super-secret-value"));
    }
}
