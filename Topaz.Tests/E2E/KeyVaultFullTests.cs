using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.Security.KeyVault.Secrets;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultFullTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("1898F130-313E-4D49-85AB-5F501F311159");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test";
    private const string TestKeyVaultName = "testkv";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.Main(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.Main(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.Main([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.Main([
            "group",
            "create",
            "--name",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.Main([
            "keyvault",
            "delete",
            "--resource-group",
            ResourceGroupName,
            "--name",
            TestKeyVaultName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsCreatedViaSDKAndSecretsAreCreated_TheyShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential();
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup("test");
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        var client = new SecretClient(vaultUri: TopazResourceHelpers.GetKeyVaultEndpoint(TestKeyVaultName), credential: credential, new SecretClientOptions()
        {
            DisableChallengeResourceVerification = true
        });
        _ = resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, TestKeyVaultName, operation, CancellationToken.None);
        
        // Act
        _ = client.SetSecret("secret-one", "test");
        var secret = client.GetSecret("secret-one");
        
        // Assert
        Assert.That(secret.Value, Is.Not.Null);
        Assert.That(secret.Value.Value, Is.EqualTo("test"));
    }
}