namespace Topaz.Tests.AzureCLI;

public class ManagedIdentityTests : TopazFixture
{
    #region Create Tests
    
    [Test]
    public async Task ManagedIdentityTests_WhenCreateCommandIsCalled_IdentityShouldBeCreatedWithCorrectProperties()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name TestIdentity123 --resource-group test-rg --location westeurope", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("TestIdentity123"));
                Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                Assert.That(response["type"]!.GetValue<string>(), Is.EqualTo("Microsoft.ManagedIdentity/userAssignedIdentities"));
                Assert.That(response["id"], Is.Not.Null);
                Assert.That(response["clientId"], Is.Not.Null);
                Assert.That(response["principalId"], Is.Not.Null);
                Assert.That(response["tenantId"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az identity delete --name TestIdentity123 --resource-group test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenResourceGroupDoesNotExist_IdentityCannotBeCreated()
    {
        await RunAzureCliCommand("az identity create --name TestIdentity --resource-group non-existent-rg --location westeurope", null, 3);
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenCreateCommandIsCalledWithTags_TagsShouldBeSet()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name TestIdentityTags --resource-group test-rg --location westeurope --tags Environment=Test Owner=Admin", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["tags"], Is.Not.Null);
                Assert.That(response["tags"]!["Environment"]!.GetValue<string>(), Is.EqualTo("Test"));
                Assert.That(response["tags"]!["Owner"]!.GetValue<string>(), Is.EqualTo("Admin"));
            });
        });
        await RunAzureCliCommand("az identity delete --name TestIdentityTags --resource-group test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenDuplicateIdentityIsCreated_ItShouldNotFail()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name DuplicateIdentity --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity create --name DuplicateIdentity --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity delete --name DuplicateIdentity --resource-group test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
    
    #region Show Tests
    
    [Test]
    public async Task ManagedIdentityTests_WhenShowCommandIsCalled_IdentityDetailsShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name ShowIdentity123 --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity show --name ShowIdentity123 --resource-group test-rg", (response) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("ShowIdentity123"));
                Assert.That(response["location"]!.GetValue<string>(), Is.EqualTo("westeurope"));
                Assert.That(response["type"]!.GetValue<string>(), Is.EqualTo("Microsoft.ManagedIdentity/userAssignedIdentities"));
                Assert.That(response["id"], Is.Not.Null);
                Assert.That(response["clientId"], Is.Not.Null);
                Assert.That(response["principalId"], Is.Not.Null);
                Assert.That(response["tenantId"], Is.Not.Null);
            });
        });
        await RunAzureCliCommand("az identity delete --name ShowIdentity123 --resource-group test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenShowCommandIsCalledForNonExistentIdentity_ItShouldFail()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity show --name NonExistentIdentity --resource-group test-rg", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
    
    #region List Tests
    
    [Test]
    public async Task ManagedIdentityTests_WhenListCommandIsCalledWithResourceGroup_IdentitiesInGroupShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name ListIdentity001 --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity create --name ListIdentity002 --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity list --resource-group test-rg", (response) =>
        {
            var identities = response.AsArray();
            Assert.Multiple(() =>
            {
                Assert.That(identities.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "ListIdentity001"), Is.True);
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "ListIdentity002"), Is.True);
            });
        });
        await RunAzureCliCommand("az identity delete --name ListIdentity001 --resource-group test-rg");
        await RunAzureCliCommand("az identity delete --name ListIdentity002 --resource-group test-rg");
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenListCommandIsCalledForSubscription_AllIdentitiesShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg-1 -l westeurope");
        await RunAzureCliCommand("az group create -n test-rg-2 -l westeurope");
        await RunAzureCliCommand("az identity create --name SubIdentity001 --resource-group test-rg-1 --location westeurope");
        await RunAzureCliCommand("az identity create --name SubIdentity002 --resource-group test-rg-2 --location westeurope");
        await RunAzureCliCommand("az identity list", (response) =>
        {
            var identities = response.AsArray();
            Assert.Multiple(() =>
            {
                Assert.That(identities.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "SubIdentity001"), Is.True);
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "SubIdentity002"), Is.True);
            });
        });
        await RunAzureCliCommand("az identity delete --name SubIdentity001 --resource-group test-rg-1");
        await RunAzureCliCommand("az identity delete --name SubIdentity002 --resource-group test-rg-2");
        await RunAzureCliCommand("az group delete -n test-rg-1 --yes");
        await RunAzureCliCommand("az group delete -n test-rg-2 --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenListCommandIsCalledWithSpecificResourceGroup_OnlyIdentitiesInGroupShouldBeReturned()
    {
        await RunAzureCliCommand("az group create -n test-rg-1 -l westeurope");
        await RunAzureCliCommand("az group create -n test-rg-2 -l westeurope");
        await RunAzureCliCommand("az identity create --name RgIdentity001 --resource-group test-rg-1 --location westeurope");
        await RunAzureCliCommand("az identity create --name RgIdentity002 --resource-group test-rg-2 --location westeurope");
        await RunAzureCliCommand("az identity list --resource-group test-rg-1", (response) =>
        {
            var identities = response.AsArray();
            Assert.Multiple(() =>
            {
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "RgIdentity001"), Is.True);
                Assert.That(identities.Any(i => i!["name"]!.GetValue<string>() == "RgIdentity002"), Is.False);
            });
        });
        await RunAzureCliCommand("az identity delete --name RgIdentity001 --resource-group test-rg-1");
        await RunAzureCliCommand("az identity delete --name RgIdentity002 --resource-group test-rg-2");
        await RunAzureCliCommand("az group delete -n test-rg-1 --yes");
        await RunAzureCliCommand("az group delete -n test-rg-2 --yes");
    }
    
    #endregion
    
    #region Delete Tests
    
    [Test]
    public async Task ManagedIdentityTests_WhenDeleteCommandIsCalled_IdentityShouldBeDeleted()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity create --name DeleteIdentity123 --resource-group test-rg --location westeurope");
        await RunAzureCliCommand("az identity delete --name DeleteIdentity123 --resource-group test-rg");
        await RunAzureCliCommand("az identity show --name DeleteIdentity123 --resource-group test-rg", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenDeleteCommandIsCalledForNonExistentIdentity_ItShouldFail()
    {
        await RunAzureCliCommand("az group create -n test-rg -l westeurope");
        await RunAzureCliCommand("az identity delete --name NonExistentIdentity --resource-group test-rg", null, 3);
        await RunAzureCliCommand("az group delete -n test-rg --yes");
    }
    
    #endregion
}