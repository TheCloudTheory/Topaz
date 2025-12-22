using Topaz.CLI;

namespace Topaz.Tests.CLI;

public class ManagedIdentityTests
{
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-123456789ABC");
    private const string SubscriptionName = "identity-sub";
    private const string ResourceGroupName = "identity-rg";
    private const string IdentityName = "MyManagedIdentity";

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

        await Program.Main([
            "identity",
            "delete",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString(),
        ]);
        
        await Program.Main([
            "identity",
            "create",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
    }

    [Test]
    public void ManagedIdentityTests_WhenNewIdentityIsRequested_ItShouldBeCreated()
    {
        var identityPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".managed-identity", IdentityName,
            "metadata.json");

        Assert.That(File.Exists(identityPath), Is.True);
    }

    [Test]
    public async Task ManagedIdentityTests_WhenNewIdentityIsDeleted_ItShouldBeDeleted()
    {
        var identityPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".managed-identity", IdentityName,
            "metadata.json");

        var result = await Program.Main([
            "identity",
            "delete",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(0));
            Assert.That(File.Exists(identityPath), Is.False);
        });
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenDeletingNonExistentIdentity_ItShouldReturnError()
    {
        var result = await Program.Main([
            "identity",
            "delete",
            "--name",
            "non-existent-identity",
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ManagedIdentityTests_WhenShowingExistingIdentity_ItShouldReturnDetails()
    {
        var result = await Program.Main([
            "identity",
            "show",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenShowingNonExistentIdentity_ItShouldReturnError()
    {
        var result = await Program.Main([
            "identity",
            "show",
            "--name",
            "non-existent-identity",
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ManagedIdentityTests_WhenListingIdentitiesByResourceGroup_ItShouldReturnList()
    {
        var result = await Program.Main([
            "identity",
            "list",
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenListingIdentitiesBySubscription_ItShouldReturnList()
    {
        var result = await Program.Main([
            "identity",
            "list",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public async Task ManagedIdentityTests_WhenDuplicatedIdentityIsAttemptedToBeCreated_ItShouldFailGracefullyWithMeaningfulError()
    {
        var result = await Program.Main([
            "identity",
            "create",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);
        
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ManagedIdentityTests_WhenCreatingIdentityWithTags_ItShouldCreateWithTags()
    {
        const string identityNameWithTags = "identity-with-tags";
        
        await Program.Main([
            "identity",
            "delete",
            "--name",
            identityNameWithTags,
            "-g",
            ResourceGroupName,
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        var result = await Program.Main([
            "identity",
            "create",
            "--name",
            identityNameWithTags,
            "-g",
            ResourceGroupName,
            "--location",
            "westeurope",
            "--tags",
            "environment=test",
            "--tags",
            "owner=admin",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
        
        var identityPath = Path.Combine(Directory.GetCurrentDirectory(), ".topaz", ".subscription",
            SubscriptionId.ToString(), ".resource-group", ResourceGroupName, ".managed-identity", identityNameWithTags,
            "metadata.json");

        Assert.That(File.Exists(identityPath), Is.True);
    }

    [Test]
    public async Task ManagedIdentityTests_WhenUpdatingIdentityTags_ItShouldUpdateSuccessfully()
    {
        var result = await Program.Main([
            "identity",
            "update",
            "--name",
            IdentityName,
            "-g",
            ResourceGroupName,
            "--tags",
            "updated=true",
            "--tags",
            "version=2",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(0));
    }
    
    [Test]
    public async Task ManagedIdentityTests_WhenUpdatingNonExistentIdentity_ItShouldReturnError()
    {
        var result = await Program.Main([
            "identity",
            "update",
            "--name",
            "non-existent-identity",
            "-g",
            ResourceGroupName,
            "--tags",
            "test=value",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task ManagedIdentityTests_WhenCreatingIdentityInNonExistentResourceGroup_ItShouldReturnError()
    {
        var result = await Program.Main([
            "identity",
            "create",
            "--name",
            "test-identity",
            "-g",
            "non-existent-rg",
            "--location",
            "westeurope",
            "--subscription-id",
            SubscriptionId.ToString()
        ]);

        Assert.That(result, Is.EqualTo(1));
    }
}