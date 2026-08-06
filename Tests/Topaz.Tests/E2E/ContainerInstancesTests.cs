using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ContainerInstancesTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("0AFBA300-2768-4F2E-8F39-FA887BF4E18E");

    private const string SubscriptionName = "sub-e2e-aci";
    private const string ResourceGroupName = "rg-e2e-aci";
    private const string ContainerGroupName = "e2e-aci-group";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static ContainerGroupData MinimalContainerGroup() =>
        new(new AzureLocation("westeurope"), [
            new ContainerInstanceContainer("app", "nginx:latest",
                new ContainerResourceRequirements(
                    new ContainerResourceRequestsContent(1.0, 1.5)))
        ])
        {
            OSType = ContainerInstanceOperatingSystemType.Linux
        };

    [Test]
    public async Task ContainerGroup_Create_ShouldReturnCreatedGroup()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var lro = await resourceGroup.Value.GetContainerGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lro.Value.Data.Name, Is.EqualTo(ContainerGroupName));
            Assert.That(lro.Value.Data.Location.ToString(), Is.EqualTo("westeurope"));
            Assert.That(lro.Value.Data.ProvisioningState, Is.EqualTo("Succeeded"));
        }
    }

    [Test]
    public async Task ContainerGroup_Get_WhenGroupExists_ShouldReturnGroup()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var groups = resourceGroup.Value.GetContainerGroups();

        await groups.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        var fetched = await groups.GetAsync(ContainerGroupName);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fetched.Value.Data.Name, Is.EqualTo(ContainerGroupName));
            Assert.That(fetched.Value.Data.OSType, Is.EqualTo(ContainerInstanceOperatingSystemType.Linux));
        }
    }

    [Test]
    public async Task ContainerGroup_Get_WhenGroupDoesNotExist_ShouldThrow()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetContainerGroups().GetAsync("nonexistent-group"));
    }

    [Test]
    public async Task ContainerGroup_ListByResourceGroup_ShouldContainCreatedGroup()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var groups = resourceGroup.Value.GetContainerGroups();

        await groups.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        var names = new List<string>();
        await foreach (var group in groups.GetAllAsync())
            names.Add(group.Data.Name);

        Assert.That(names, Does.Contain(ContainerGroupName));
    }

    [Test]
    public async Task ContainerGroup_ListBySubscription_ShouldContainCreatedGroup()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        await resourceGroup.Value.GetContainerGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        var names = new List<string>();
        await foreach (var group in subscription.GetContainerGroupsAsync())
            names.Add(group.Data.Name);

        Assert.That(names, Does.Contain(ContainerGroupName));
    }

    [Test]
    public async Task ContainerGroup_Update_ShouldPersistChanges()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var groups = resourceGroup.Value.GetContainerGroups();

        await groups.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        var updated = MinimalContainerGroup();
        updated.RestartPolicy = ContainerGroupRestartPolicy.OnFailure;
        await groups.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, updated);

        var fetched = await groups.GetAsync(ContainerGroupName);
        Assert.That(fetched.Value.Data.RestartPolicy, Is.EqualTo(ContainerGroupRestartPolicy.OnFailure));
    }

    [Test]
    public async Task ContainerGroup_Start_ShouldSucceedWithoutError()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var lro = await resourceGroup.Value.GetContainerGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        Assert.DoesNotThrowAsync(async () =>
            await lro.Value.StartAsync(WaitUntil.Completed));
    }

    [Test]
    public async Task ContainerGroup_Stop_ShouldSucceedWithoutError()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var lro = await resourceGroup.Value.GetContainerGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        Assert.DoesNotThrowAsync(async () =>
            await lro.Value.StopAsync());
    }

    [Test]
    public async Task ContainerGroup_Restart_ShouldSucceedWithoutError()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var lro = await resourceGroup.Value.GetContainerGroups()
            .CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());

        Assert.DoesNotThrowAsync(async () =>
            await lro.Value.RestartAsync(WaitUntil.Completed));
    }

    [Test]
    public async Task ContainerGroup_Delete_ShouldRemoveGroup()
    {
        var client = CreateClient();
        var subscription = await client.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var groups = resourceGroup.Value.GetContainerGroups();

        var lro = await groups.CreateOrUpdateAsync(WaitUntil.Completed, ContainerGroupName, MinimalContainerGroup());
        await lro.Value.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await groups.GetAsync(ContainerGroupName));
    }
}
