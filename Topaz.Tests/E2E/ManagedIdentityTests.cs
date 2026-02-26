using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ManagedServiceIdentities;
using Microsoft.Graph;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ManagedIdentityTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("B1C2D3E4-F5A6-4B7C-8D9E-123456789DEF");

    private static GraphServiceClient GraphClient => new(new HttpClient(), new LocalGraphAuthenticationProvider(),
        "https://topaz.local.dev:8899");
    
    private const string SubscriptionName = "sub-test-identity";
    private const string ResourceGroupName = "rg-test-identity";
    
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

    [Test]
    public void ManagedIdentityTests_WhenIdentityIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        const string testIdentityName = "testidentityusingsdk";
        
        // Act
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(testIdentityName);
        
        // Assert
        Assert.That(identity, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(identity.Value.Data.Name, Is.EqualTo(testIdentityName));
            Assert.That(identity.Value.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.ManagedIdentity/userAssignedIdentities")));
            Assert.That(identity.Value.Data.Location, Is.EqualTo(AzureLocation.WestEurope));
            Assert.That(identity.Value.Data.ClientId, Is.Not.Null);
            Assert.That(identity.Value.Data.PrincipalId, Is.Not.Null);
            Assert.That(identity.Value.Data.TenantId, Is.Not.Null);
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenIdentityIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        const string testIdentityName = "testidentitydeleted";
        
        // Act
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(testIdentityName);
        identity.Value.Delete(WaitUntil.Completed);
        
        // Assert
        Assert.Throws<RequestFailedException>(() => resourceGroup.Value.GetUserAssignedIdentity(testIdentityName));
    }

    [Test]
    public void ManagedIdentityTests_WhenIdentityIsCreatedWithTags_TheTagsShouldBePersisted()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope)
        {
            Tags =
            {
                ["environment"] = "test",
                ["owner"] = "admin",
                ["version"] = "1.0"
            }
        };
        const string testIdentityName = "testidentitywithtags";
        
        // Act
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(testIdentityName);
        
        // Assert
        Assert.That(identity, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(identity.Value.Data.Name, Is.EqualTo(testIdentityName));
            Assert.That(identity.Value.Data.Tags, Is.Not.Empty);
            Assert.That(identity.Value.Data.Tags, Has.Count.EqualTo(3));
            Assert.That(identity.Value.Data.Tags["environment"], Is.EqualTo("test"));
            Assert.That(identity.Value.Data.Tags["owner"], Is.EqualTo("admin"));
            Assert.That(identity.Value.Data.Tags["version"], Is.EqualTo("1.0"));
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenIdentityIsUpdated_ThePropertiesShouldBeUpdated()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope)
        {
            Tags =
            {
                ["environment"] = "dev"
            }
        };
        const string testIdentityName = "testidentityupdated";
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);
        
        var updateOperation = new UserAssignedIdentityData(AzureLocation.WestEurope)
        {
            Tags =
            {
                ["environment"] = "production",
                ["team"] = "devops"
            }
        };
        
        // Act
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, updateOperation, CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(testIdentityName);
        
        // Assert
        Assert.That(identity, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(identity.Value.Data.Name, Is.EqualTo(testIdentityName));
            Assert.That(identity.Value.Data.Tags, Is.Not.Empty);
            Assert.That(identity.Value.Data.Tags, Has.Count.EqualTo(2));
            Assert.That(identity.Value.Data.Tags["environment"], Is.EqualTo("production"));
            Assert.That(identity.Value.Data.Tags["team"], Is.EqualTo("devops"));
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenListingIdentitiesInResourceGroup_AllIdentitiesShouldBeReturned()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        
        const string identity1 = "testidentity1";
        const string identity2 = "testidentity2";
        const string identity3 = "testidentity3";
        
        // Act
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identity1, operation, CancellationToken.None);
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identity2, operation, CancellationToken.None);
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identity3, operation, CancellationToken.None);
        
        var identities = identityCollection.GetAll().ToList();
        
        // Assert
        Assert.That(identities, Is.Not.Empty);
        Assert.That(identities, Has.Count.GreaterThanOrEqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(identities.Any(i => i.Data.Name == identity1), Is.True);
            Assert.That(identities.Any(i => i.Data.Name == identity2), Is.True);
            Assert.That(identities.Any(i => i.Data.Name == identity3), Is.True);
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenListingIdentitiesBySubscription_AllIdentitiesShouldBeReturned()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        
        const string identitySub1 = "testidentitysub1";
        const string identitySub2 = "testidentitysub2";
        
        // Act
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identitySub1, operation, CancellationToken.None);
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identitySub2, operation, CancellationToken.None);
        
        var identities = subscription.GetUserAssignedIdentities().ToList();
        
        // Assert
        Assert.That(identities, Is.Not.Empty);
        Assert.That(identities, Has.Count.GreaterThanOrEqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(identities.Any(i => i.Data.Name == identitySub1), Is.True);
            Assert.That(identities.Any(i => i.Data.Name == identitySub2), Is.True);
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenIdentityHasGeneratedProperties_TheyShouldBeUnique()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        const string testIdentityName = "testidentityprops";
        
        // Act
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        identityCollection.CreateOrUpdate(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(testIdentityName);
        
        // Assert
        Assert.That(identity, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(identity.Value.Data.ClientId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(identity.Value.Data.PrincipalId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(identity.Value.Data.TenantId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(identity.Value.Data.ClientId, Is.Not.EqualTo(identity.Value.Data.PrincipalId));
        });
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenIdentityIsCreatedUsingSDK_ServicePrincipalShouldAlsoExist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var operation = new UserAssignedIdentityData(AzureLocation.WestEurope);
        var testIdentityName = $"testidentity-sp-{Guid.NewGuid():N}".ToLowerInvariant();

        // Act (create managed identity via ARM SDK)
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        await identityCollection.CreateOrUpdateAsync(WaitUntil.Completed, testIdentityName, operation, CancellationToken.None);

        var identity = await resourceGroup.Value.GetUserAssignedIdentityAsync(testIdentityName);
        var clientId = identity.Value.Data.ClientId;

        // Assert (managed identity has a client id)
        Assert.That(clientId, Is.Not.Null);
        Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));

        // Assert (service principal exists in Entra for that appId/clientId)
        var servicePrincipal = await WaitForServicePrincipalByAppIdAsync(GraphClient, clientId.Value.ToString());
        Assert.That(servicePrincipal, Is.Not.Null);
        Assert.That(servicePrincipal!.AppId, Is.EqualTo(clientId.Value.ToString()));
    }
    
    private static async Task<Microsoft.Graph.Models.ServicePrincipal?> WaitForServicePrincipalByAppIdAsync(
        GraphServiceClient client,
        string appId,
        int maxAttempts = 20,
        int delayMs = 250)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var result = await client.ServicePrincipals[appId].GetAsync();
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(delayMs);
        }

        Assert.Fail($"Service principal with appId '{appId}' was not found after {maxAttempts} attempts.");
        return null;
    }
}