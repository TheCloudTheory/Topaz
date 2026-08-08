namespace Topaz.Tests.AzureCLI;

public class ApiManagementTests : TopazFixture
{
    [Test]
    public async Task ApiManagementService_Create_Show_List_Update_And_Delete()
    {
        var serviceName = $"topazapim{Guid.NewGuid():N}"[..30];
        const string resourceGroup = "test-apim-rg";
        string serviceId = null!;

        await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");

        // Create
        await RunAzureCliCommand(
            $"az apim create --name {serviceName} --resource-group {resourceGroup} --location eastus --publisher-email admin@topaz.local --publisher-name Topaz --sku-name Developer",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(serviceName));
                    Assert.That(resp["provisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
                    Assert.That(resp["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("Developer"));
                });
                serviceId = resp["id"]!.GetValue<string>();
            });

        // Show
        await RunAzureCliCommand(
            $"az apim show --name {serviceName} --resource-group {resourceGroup}",
            (resp) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(serviceName));
                    Assert.That(resp["id"]!.GetValue<string>(), Is.EqualTo(serviceId));
                });
            });

        // List by resource group
        await RunAzureCliCommand(
            $"az apim list --resource-group {resourceGroup}",
            (resp) =>
            {
                var arr = resp.AsArray();
                Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == serviceName), Is.True);
            });

        // Update (add tags)
        await RunAzureCliCommand(
            $"az apim update --name {serviceName} --resource-group {resourceGroup} --set tags.environment=test tags.purpose=testing",
            (resp) =>
            {
                var tags = resp["tags"]!.AsObject();
                Assert.Multiple(() =>
                {
                    Assert.That(tags["environment"]!.GetValue<string>(), Is.EqualTo("test"));
                    Assert.That(tags["purpose"]!.GetValue<string>(), Is.EqualTo("testing"));
                });
            });

        // Delete
        await RunAzureCliCommand($"az apim delete --name {serviceName} --resource-group {resourceGroup} --yes");

        // Verify deletion
        await RunAzureCliCommand(
            $"az apim list --resource-group {resourceGroup}",
            (resp) =>
            {
                var arr = resp.AsArray();
                Assert.That(arr.Any(a => a!["name"]!.GetValue<string>() == serviceName), Is.False);
            });

        await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
    }

    [Test]
    public async Task ApiManagementService_CheckName_Available()
    {
        var serviceName = $"topazapim{Guid.NewGuid():N}"[..30];

        await RunAzureCliCommand(
            $"az apim check-name --name {serviceName}",
            (resp) =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.True);
            });
    }

    [Test]
    public async Task ApiManagementService_CheckName_NotAvailable_AfterCreate()
    {
        var serviceName = $"topazapim{Guid.NewGuid():N}"[..30];
        const string resourceGroup = "test-apim-checkname-rg";

        await RunAzureCliCommand($"az group create --name {resourceGroup} --location eastus");
        await RunAzureCliCommand(
            $"az apim create --name {serviceName} --resource-group {resourceGroup} --location eastus --publisher-email admin@topaz.local --publisher-name Topaz --sku-name Developer");

        await RunAzureCliCommand(
            $"az apim check-name --name {serviceName}",
            (resp) =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.False);
            });

        await RunAzureCliCommand($"az apim delete --name {serviceName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete --name {resourceGroup} --yes");
    }
}
