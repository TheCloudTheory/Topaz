using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Resources;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class VirtualMachineTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

    private const string SubscriptionName = "sub-test-vm";
    private const string ResourceGroupName = "rg-test-vm";

    private ResourceIdentifier _nicId = null!;

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(
        [
            "subscription", "delete",
            "--id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "subscription", "create",
            "--id", SubscriptionId.ToString(),
            "--name", SubscriptionName
        ]);

        await Program.RunAsync(
        [
            "group", "delete",
            "--name", ResourceGroupName,
            "--subscription-id", SubscriptionId.ToString()
        ]);

        await Program.RunAsync(
        [
            "group", "create",
            "--name", ResourceGroupName,
            "--location", "westeurope",
            "--subscription-id", SubscriptionId.ToString()
        ]);

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var vnetData = new VirtualNetworkData
        {
            Location = AzureLocation.WestEurope,
            AddressPrefixes = { "10.20.0.0/16" },
            Subnets =
            {
                new SubnetData { Name = "default", AddressPrefixes = { "10.20.0.0/24" } }
            }
        };
        await resourceGroup.Value.GetVirtualNetworks()
            .CreateOrUpdateAsync(WaitUntil.Completed, "vnet-test-vm", vnetData);

        var nicData = new NetworkInterfaceData
        {
            Location = AzureLocation.WestEurope,
            IPConfigurations =
            {
                new NetworkInterfaceIPConfigurationData
                {
                    Name = "ipconfig1",
                    PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Subnet = new SubnetData
                    {
                        Id = new ResourceIdentifier(
                            $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Network/virtualNetworks/vnet-test-vm/subnets/default")
                    }
                }
            }
        };
        var nic = await resourceGroup.Value.GetNetworkInterfaces()
            .CreateOrUpdateAsync(WaitUntil.Completed, "nic-test-vm", nicData);
        _nicId = nic.Value.Data.Id;
    }

    private VirtualMachineData MinimalVmData(string computerName) =>
        new(AzureLocation.WestEurope)
        {
            HardwareProfile = new VirtualMachineHardwareProfile
            {
                VmSize = VirtualMachineSizeType.StandardD2SV3
            },
            OSProfile = new VirtualMachineOSProfile
            {
                ComputerName = computerName,
                AdminUsername = "adminuser",
                AdminPassword = "Admin1234!@#"
            },
            StorageProfile = new VirtualMachineStorageProfile
            {
                ImageReference = new ImageReference
                {
                    Publisher = "Canonical",
                    Offer = "0001-com-ubuntu-server-jammy",
                    Sku = "22_04-lts",
                    Version = "latest"
                }
            },
            NetworkProfile = new VirtualMachineNetworkProfile
            {
                NetworkInterfaces =
                {
                    new VirtualMachineNetworkInterfaceReference
                    {
                        Primary = true,
                        Id = _nicId
                    }
                }
            }
        };

    [Test]
    public async Task VirtualMachineTests_WhenVMIsCreatedUsingSDK_ItShouldBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string vmName = "test-vm-create";

        // Act
        var createResult = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, vmName, MinimalVmData(vmName));

        var vm = createResult.Value;

        // Assert
        Assert.That(vm, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(vm.Data.Name, Is.EqualTo(vmName));
            Assert.That(vm.Data.ResourceType, Is.EqualTo(new ResourceType("Microsoft.Compute/virtualMachines")));
            Assert.That(vm.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
            Assert.That(vm.Data.ProvisioningState, Is.EqualTo("Succeeded"));
            Assert.That(vm.Data.VmId, Is.Not.Null);
        });
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMIsDeletedUsingSDK_ItShouldNotBeAvailable()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string vmName = "test-vm-delete";

        await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, vmName, MinimalVmData(vmName));

        // Act
        var vm = resourceGroup.Value.GetVirtualMachine(vmName);
        await vm.Value.DeleteAsync(WaitUntil.Completed);

        // Assert
        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.Value.GetVirtualMachineAsync(vmName));
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMTagsAreUpdated_TagsShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string vmName = "test-vm-update";

        await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, vmName, MinimalVmData(vmName));

        var updatedData = MinimalVmData(vmName);
        updatedData.Tags.Add("env", "test");
        updatedData.Tags.Add("team", "platform");

        // Act
        var updateResult = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, vmName, updatedData);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("env").WithValue("test"));
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("team").WithValue("platform"));
        });
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMsAreListedByResourceGroup_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var resultA = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-vm-list-a", MinimalVmData("test-vm-list-a"));
        Assert.That(resultA.Value.Data.Name, Is.EqualTo("test-vm-list-a"));

        var resultB = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-vm-list-b", MinimalVmData("test-vm-list-b"));
        Assert.That(resultB.Value.Data.Name, Is.EqualTo("test-vm-list-b"));

        // Act
        var vms = new List<VirtualMachineResource>();
        await foreach (var vm in resourceGroup.Value.GetVirtualMachines().GetAllAsync())
            vms.Add(vm);

        // Assert
        var names = vms.Select(v => v.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-vm-list-a"));
            Assert.That(names, Does.Contain("test-vm-list-b"));
        });
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMsAreListedBySubscription_AllShouldAppear()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);

        var resultA = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-vm-sub-a", MinimalVmData("test-vm-sub-a"));
        Assert.That(resultA.Value.Data.Name, Is.EqualTo("test-vm-sub-a"));

        var resultB = await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, "test-vm-sub-b", MinimalVmData("test-vm-sub-b"));
        Assert.That(resultB.Value.Data.Name, Is.EqualTo("test-vm-sub-b"));

        // Act
        var vms = new List<VirtualMachineResource>();
        await foreach (var vm in subscription.GetVirtualMachinesAsync())
            vms.Add(vm);

        // Assert
        var names = vms.Select(v => v.Data.Name).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(names, Does.Contain("test-vm-sub-a"));
            Assert.That(names, Does.Contain("test-vm-sub-b"));
        });
    }

    [Test]
    public async Task VirtualMachineTests_WhenVMIsUpdatedWithPatch_TagsAndHardwareProfileShouldPersist()
    {
        // Arrange
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        const string vmName = "test-vm-patch";

        await resourceGroup.Value.GetVirtualMachines()
            .CreateOrUpdateAsync(WaitUntil.Completed, vmName, MinimalVmData(vmName));

        var patch = new VirtualMachinePatch();
        patch.Tags.Add("env", "staging");
        patch.Tags.Add("team", "platform");
        patch.HardwareProfile = new VirtualMachineHardwareProfile
        {
            VmSize = VirtualMachineSizeType.StandardD4SV3
        };

        // Act
        var vm = resourceGroup.Value.GetVirtualMachine(vmName);
        var updateResult = await vm.Value.UpdateAsync(WaitUntil.Completed, patch);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("env").WithValue("staging"));
            Assert.That(updateResult.Value.Data.Tags, Does.ContainKey("team").WithValue("platform"));
            Assert.That(updateResult.Value.Data.HardwareProfile.VmSize, Is.EqualTo(VirtualMachineSizeType.StandardD4SV3));
            Assert.That(updateResult.Value.Data.OSProfile, Is.Not.Null);
            Assert.That(updateResult.Value.Data.NetworkProfile, Is.Not.Null);
        });
    }
}
