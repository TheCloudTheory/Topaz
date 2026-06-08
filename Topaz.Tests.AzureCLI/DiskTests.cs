namespace Topaz.Tests.AzureCLI;

public class DiskTests : TopazFixture
{
    private const string ResourceGroup = "rg-cli-disk";
    private const string DiskName = "my-cli-disk";

    [Test]
    public async Task DiskTests_WhenDiskIsCreated_ItShouldBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup} --name {DiskName} --location westeurope " +
            $"--properties '{{\"diskSizeGB\":32,\"creationData\":{{\"createOption\":\"Empty\"}}}}'",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo(DiskName));
                    Assert.That(response["type"]!.GetValue<string>(),
                        Is.EqualTo("Microsoft.Compute/disks").IgnoreCase);
                    Assert.That(response["properties"]!["provisioningState"]!.GetValue<string>(),
                        Is.EqualTo("Succeeded"));
                });
            }, 0);
    }

    [Test]
    public async Task DiskTests_WhenDiskIsDeleted_ItShouldNotBeAvailable()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-del", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-del --name {DiskName}-del --location westeurope " +
            $"--properties '{{\"diskSizeGB\":32,\"creationData\":{{\"createOption\":\"Empty\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource delete --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-del --name {DiskName}-del",
            null, 0);
        await RunAzureCliCommand(
            $"az resource show --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-del --name {DiskName}-del",
            null, 3);
    }

    [Test]
    public async Task DiskTests_WhenDisksAreListed_AllShouldAppear()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-list", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-list --name {DiskName}-list-a --location westeurope " +
            $"--properties '{{\"diskSizeGB\":32,\"creationData\":{{\"createOption\":\"Empty\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-list --name {DiskName}-list-b --location westeurope " +
            $"--properties '{{\"diskSizeGB\":32,\"creationData\":{{\"createOption\":\"Empty\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource list --resource-group {ResourceGroup}-list " +
            $"--resource-type Microsoft.Compute/disks",
            response =>
            {
                var array = response.AsArray()!;
                var names = array.Select(n => n!["name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain($"{DiskName}-list-a"));
                    Assert.That(names, Does.Contain($"{DiskName}-list-b"));
                });
            }, 0);
    }

    [Test]
    public async Task DiskTests_WhenDiskIsUpdatedWithPatch_TagsShouldPersist()
    {
        await RunAzureCliCommand($"az group create -l westeurope -n {ResourceGroup}-patch", null, 0);
        await RunAzureCliCommand(
            $"az resource create --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-patch --name {DiskName}-patch --location westeurope " +
            $"--properties '{{\"diskSizeGB\":32,\"creationData\":{{\"createOption\":\"Empty\"}}}}'",
            null, 0);
        await RunAzureCliCommand(
            $"az resource patch --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-patch --name {DiskName}-patch " +
            $"--properties '{{}}' --latest-include-preview",
            null, 0);
        await RunAzureCliCommand(
            $"az resource show --resource-type Microsoft.Compute/disks --api-version 2025-11-01 " +
            $"--resource-group {ResourceGroup}-patch --name {DiskName}-patch",
            response =>
            {
                Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo($"{DiskName}-patch"));
            }, 0);
    }
}
