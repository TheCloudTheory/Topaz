using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.Resources;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class KeyVaultServiceTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("727D20F8-F051-41D0-8D00-E93D31E998E8");
    
    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "rg-test-keyvault";
    
    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription",
            "delete",
            "--id",
            SubscriptionId.ToString()
        ]);
        
        await Program.RunAsync(
        [
            "subscription",
            "create",
            "--id",
            SubscriptionId.ToString(),
            "--name",
            SubscriptionName
        ]);

        await Program.RunAsync([
            "group",
            "delete",
            "--name",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        await Program.RunAsync([
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

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvusingsdk";
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        
        // Assert
        Assert.That(kv, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(kv.Value.Data.Name, Is.EqualTo(testKeyVaultName));
            Assert.That(kv.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.KeyVault/vaults")));
            Assert.That(kv.Value.Data.Properties.TenantId, Is.EqualTo(operation.Properties.TenantId));
            Assert.That(kv.Value.Data.Properties.Sku.Family, Is.EqualTo(operation.Properties.Sku.Family));
            Assert.That(kv.Value.Data.Properties.Sku.Name, Is.EqualTo(operation.Properties.Sku.Name));
        });
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvdeleted";
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        kv.Value.Delete(WaitUntil.Completed);
        
        // Assert
        Assert.Throws<RequestFailedException>(() => resourceGroup.Value.GetKeyVault(testKeyVaultName));
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsUpdatedUsingSDK_TheProvidedPropertiesShouldBeUpdated()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvupdated";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var updateOperation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))
            {
                EnabledForDeployment = true,
                EnabledForDiskEncryption =  true,
                EnabledForTemplateDeployment = true,
                EnableRbacAuthorization = true,
            });
        
        // Act
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, updateOperation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        
        // Assert
        Assert.That(kv, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(kv.Value.Data.Name, Is.EqualTo(testKeyVaultName));
            Assert.That(kv.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.KeyVault/vaults")));
            Assert.That(kv.Value.Data.Properties.TenantId, Is.EqualTo(operation.Properties.TenantId));
            Assert.That(kv.Value.Data.Properties.Sku.Family, Is.EqualTo(operation.Properties.Sku.Family));
            Assert.That(kv.Value.Data.Properties.Sku.Name, Is.EqualTo(operation.Properties.Sku.Name));
            Assert.That(kv.Value.Data.Properties.EnabledForDeployment, Is.EqualTo(updateOperation.Properties.EnabledForDeployment));
            Assert.That(kv.Value.Data.Properties.EnabledForDiskEncryption, Is.EqualTo(updateOperation.Properties.EnabledForDiskEncryption));
            Assert.That(kv.Value.Data.Properties.EnabledForTemplateDeployment, Is.EqualTo(updateOperation.Properties.EnabledForTemplateDeployment));
            Assert.That(kv.Value.Data.Properties.EnableRbacAuthorization, Is.EqualTo(updateOperation.Properties.EnableRbacAuthorization));
        });
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultIsDeletedAndThenRecovered_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvrecovered";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var originalVault = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        _ = originalVault.Value.Delete(WaitUntil.Completed);
        var updateOperation = new
        {
            location = AzureLocation.WestEurope.ToString(),
            properties = new
            {
                tenantId = operation.Properties.TenantId,
                sku = operation.Properties.Sku.Family,
                createMode = "recover"
            }
        };
        
        // Act
        armClient.GetGenericResource(originalVault.Value.Id).Update(WaitUntil.Completed, new GenericResourceData(AzureLocation.WestEurope)
        {
            Properties = BinaryData.FromObjectAsJson(updateOperation.properties)
        });
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        
        // Assert
        Assert.That(kv, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(kv.Value.Data.Name, Is.EqualTo(testKeyVaultName));
            Assert.That(kv.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.KeyVault/vaults")));
            Assert.That(kv.Value.Data.Properties.TenantId, Is.EqualTo(operation.Properties.TenantId));
            Assert.That(kv.Value.Data.Properties.Sku.Family, Is.EqualTo(operation.Properties.Sku.Family));
            Assert.That(kv.Value.Data.Properties.Sku.Name, Is.EqualTo(operation.Properties.Sku.Name));
        });
    }

    [Test]
    public void KeyVaultTests_WhenAccessPolicyIsAdded_ItShouldAppearInVaultProperties()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvpolicyadd";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        var objectId = Guid.NewGuid();

        var policyParams = new KeyVaultAccessPolicyParameters(
            new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, objectId.ToString(),
                    new IdentityAccessPermissions
                    {
                        Secrets = { IdentityAccessSecretPermission.Get, IdentityAccessSecretPermission.List }
                    })
            ]));

        // Act
        kv.Value.UpdateAccessPolicy(AccessPolicyUpdateKind.Add, policyParams);
        var updatedKv = resourceGroup.Value.GetKeyVault(testKeyVaultName);

        // Assert
        Assert.That(updatedKv.Value.Data.Properties.AccessPolicies, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(updatedKv.Value.Data.Properties.AccessPolicies[0].ObjectId, Is.EqualTo(objectId.ToString()));
            Assert.That(updatedKv.Value.Data.Properties.AccessPolicies[0].Permissions.Secrets, Does.Contain(IdentityAccessSecretPermission.Get));
            Assert.That(updatedKv.Value.Data.Properties.AccessPolicies[0].Permissions.Secrets, Does.Contain(IdentityAccessSecretPermission.List));
        });
    }

    [Test]
    public void KeyVaultTests_WhenAccessPoliciesAreReplaced_OnlyNewPoliciesShouldRemain()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvpolicyrepl";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        var firstObjectId = Guid.NewGuid();
        var secondObjectId = Guid.NewGuid();

        kv.Value.UpdateAccessPolicy(AccessPolicyUpdateKind.Add,
            new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, firstObjectId.ToString(),
                    new IdentityAccessPermissions { Secrets = { IdentityAccessSecretPermission.Get } })
            ])));

        // Act — replace with a single new policy
        kv.Value.UpdateAccessPolicy(AccessPolicyUpdateKind.Replace,
            new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, secondObjectId.ToString(),
                    new IdentityAccessPermissions { Secrets = { IdentityAccessSecretPermission.List } })
            ])));

        var updatedKv = resourceGroup.Value.GetKeyVault(testKeyVaultName);

        // Assert
        Assert.That(updatedKv.Value.Data.Properties.AccessPolicies, Has.Count.EqualTo(1));
        Assert.That(updatedKv.Value.Data.Properties.AccessPolicies[0].ObjectId, Is.EqualTo(secondObjectId.ToString()));
    }

    [Test]
    public void KeyVaultTests_WhenAccessPolicyIsRemoved_ItShouldNoLongerAppearInVaultProperties()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName = "testkvpolicyrem";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName, operation, CancellationToken.None);
        var kv = resourceGroup.Value.GetKeyVault(testKeyVaultName);
        var objectId = Guid.NewGuid();
        kv.Value.UpdateAccessPolicy(AccessPolicyUpdateKind.Add,
            new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, objectId.ToString(),
                    new IdentityAccessPermissions { Secrets = { IdentityAccessSecretPermission.Get } })
            ])));

        // Act
        kv.Value.UpdateAccessPolicy(AccessPolicyUpdateKind.Remove,
            new KeyVaultAccessPolicyParameters(new KeyVaultAccessPolicyProperties(
            [
                new KeyVaultAccessPolicy(Guid.Empty, objectId.ToString(), new IdentityAccessPermissions())
            ])));

        var updatedKv = resourceGroup.Value.GetKeyVault(testKeyVaultName);

        // Assert
        Assert.That(updatedKv.Value.Data.Properties.AccessPolicies, Is.Empty);
    }

    [Test]
    public void KeyVaultTests_WhenKeyVaultsExistInSubscription_ListBySubscriptionShouldReturnThem()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
            new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard)));
        const string testKeyVaultName1 = "testkvlistsub1";
        const string testKeyVaultName2 = "testkvlistsub2";
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName1, operation, CancellationToken.None);
        resourceGroup.Value.GetKeyVaults()
            .CreateOrUpdate(WaitUntil.Completed, testKeyVaultName2, operation, CancellationToken.None);

        // Act
        var vaults = subscription.GetKeyVaults().ToList();

        // Assert
        Assert.That(vaults, Is.Not.Empty);
        var names = vaults.Select(v => v.Data.Name).ToList();
        Assert.That(names, Does.Contain(testKeyVaultName1));
        Assert.That(names, Does.Contain(testKeyVaultName2));
    }
}