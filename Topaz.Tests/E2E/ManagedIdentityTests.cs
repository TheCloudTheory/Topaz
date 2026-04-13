using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ManagedServiceIdentities;
using Microsoft.Graph;
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
        var principalId = identity.Value.Data.PrincipalId;

        // Assert (managed identity has expected properties)
        Assert.That(clientId, Is.Not.Null);
        Assert.That(clientId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(principalId, Is.Not.Null);
        Assert.That(principalId, Is.Not.EqualTo(Guid.Empty));

        // Assert (service principal exists in Entra — look up by principalId which is the SP's object ID)
        var servicePrincipal = await GraphClient.ServicePrincipals[principalId!.Value.ToString()].GetAsync();
        Assert.That(servicePrincipal, Is.Not.Null);
        Assert.That(servicePrincipal!.AppId, Is.EqualTo(clientId!.Value.ToString()));
    }

    [Test]
    public void ManagedIdentityTests_WhenFederatedCredentialIsCreated_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        const string identityName = "testidentity-fic-create";
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope), CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(identityName).Value;

        var ficData = new FederatedIdentityCredentialData
        {
            Issuer = "https://token.actions.githubusercontent.com",
            Subject = "repo:myorg/myrepo:ref:refs/heads/main",
            Audiences = { "api://AzureADTokenExchange" }
        };
        const string ficName = "my-fic";

        // Act
        var ficCollection = identity.GetFederatedIdentityCredentials();
        ficCollection.CreateOrUpdate(WaitUntil.Completed, ficName, ficData, CancellationToken.None);
        var fic = identity.GetFederatedIdentityCredential(ficName).Value;

        // Assert
        Assert.That(fic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fic.Data.Name, Is.EqualTo(ficName));
            Assert.That(fic.Data.Issuer, Is.EqualTo(ficData.Issuer));
            Assert.That(fic.Data.Subject, Is.EqualTo(ficData.Subject));
            Assert.That(fic.Data.Audiences, Has.Count.EqualTo(1));
            Assert.That(fic.Data.Audiences[0], Is.EqualTo("api://AzureADTokenExchange"));
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenFederatedCredentialIsDeleted_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        const string identityName = "testidentity-fic-delete";
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope), CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(identityName).Value;

        var ficData = new FederatedIdentityCredentialData
        {
            Issuer = "https://token.actions.githubusercontent.com",
            Subject = "repo:myorg/myrepo:ref:refs/heads/main",
            Audiences = { "api://AzureADTokenExchange" }
        };
        const string ficName = "fic-to-delete";
        identity.GetFederatedIdentityCredentials()
            .CreateOrUpdate(WaitUntil.Completed, ficName, ficData, CancellationToken.None);

        // Act
        identity.GetFederatedIdentityCredential(ficName).Value.Delete(WaitUntil.Completed);

        // Assert
        Assert.Throws<RequestFailedException>(() => identity.GetFederatedIdentityCredential(ficName).Value.Get());
    }

    [Test]
    public void ManagedIdentityTests_WhenFederatedCredentialsAreListed_AllShouldBeReturned()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        const string identityName = "testidentity-fic-list";
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope), CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(identityName).Value;
        var ficCollection = identity.GetFederatedIdentityCredentials();

        // Act
        ficCollection.CreateOrUpdate(WaitUntil.Completed, "fic-list-1", new FederatedIdentityCredentialData
        {
            Issuer = "https://token.actions.githubusercontent.com",
            Subject = "repo:myorg/repo1:ref:refs/heads/main",
            Audiences = { "api://AzureADTokenExchange" }
        }, CancellationToken.None);
        ficCollection.CreateOrUpdate(WaitUntil.Completed, "fic-list-2", new FederatedIdentityCredentialData
        {
            Issuer = "https://token.actions.githubusercontent.com",
            Subject = "repo:myorg/repo2:ref:refs/heads/main",
            Audiences = { "api://AzureADTokenExchange" }
        }, CancellationToken.None);

        var fics = ficCollection.GetAll().ToList();

        // Assert
        Assert.That(fics, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(fics.Any(f => f.Data.Name == "fic-list-1"), Is.True);
            Assert.That(fics.Any(f => f.Data.Name == "fic-list-2"), Is.True);
        });
    }

    [Test]
    public void ManagedIdentityTests_WhenFederatedCredentialIsUpdated_ThePropertiesShouldChange()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = armClient.GetDefaultSubscription();
        var resourceGroup = subscription.GetResourceGroup(ResourceGroupName);
        var identityCollection = resourceGroup.Value.GetUserAssignedIdentities();
        const string identityName = "testidentity-fic-update";
        identityCollection.CreateOrUpdate(WaitUntil.Completed, identityName,
            new UserAssignedIdentityData(AzureLocation.WestEurope), CancellationToken.None);
        var identity = resourceGroup.Value.GetUserAssignedIdentity(identityName).Value;
        const string ficName = "fic-to-update";
        identity.GetFederatedIdentityCredentials().CreateOrUpdate(WaitUntil.Completed, ficName,
            new FederatedIdentityCredentialData
            {
                Issuer = "https://token.actions.githubusercontent.com",
                Subject = "repo:myorg/myrepo:ref:refs/heads/main",
                Audiences = { "api://AzureADTokenExchange" }
            }, CancellationToken.None);

        // Act — update the subject
        identity.GetFederatedIdentityCredentials().CreateOrUpdate(WaitUntil.Completed, ficName,
            new FederatedIdentityCredentialData
            {
                Issuer = "https://token.actions.githubusercontent.com",
                Subject = "repo:myorg/myrepo:environment:production",
                Audiences = { "api://AzureADTokenExchange" }
            }, CancellationToken.None);

        var updated = identity.GetFederatedIdentityCredential(ficName).Value;

        // Assert
        Assert.That(updated.Data.Subject, Is.EqualTo("repo:myorg/myrepo:environment:production"));
    }

    [Test]
    public void ManagedIdentityTests_WhenGettingSystemAssignedIdentityThatDoesNotExist_ItShouldReturn404()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);

        var parentResourceId = new ResourceIdentifier(
            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Compute/virtualMachines/test-vm-no-identity");
        var identityResourceId = SystemAssignedIdentityResource.CreateResourceIdentifier(parentResourceId.ToString());

        // Act & Assert — identity was never enabled, expect 404
        Assert.Throws<RequestFailedException>(() =>
            armClient.GetSystemAssignedIdentityResource(identityResourceId).Get());
    }
}