using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ApiManagementTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003300");
    private const string SubscriptionName = "sub-test-apim";
    private const string ResourceGroupName = "rg-test-apim";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    private ArmClient CreateArmClient() =>
        new(new AzureLocalCredential(Globals.GlobalAdminId), SubscriptionId.ToString(), ArmClientOptions);

    private static ApiManagementServiceData MinimalServiceData() =>
        new(AzureLocation.WestEurope,
            new ApiManagementServiceSkuProperties(ApiManagementServiceSkuType.Developer, 1),
            "admin@example.com",
            "Test Publisher");

    private async Task<ResourceGroupResource> GetResourceGroupAsync(ArmClient armClient)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        return (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;
    }

    [Test]
    public async Task ApiManagementService_WhenCreated_HasCorrectProperties()
    {
        const string serviceName = "apim-create-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        var result = await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(serviceName));
            Assert.That(result.Value.Data.ResourceType.ToString(), Is.EqualTo("Microsoft.ApiManagement/service").IgnoreCase);
            Assert.That(result.Value.Data.Location.ToString(), Is.EqualTo("westeurope").IgnoreCase);
        }
    }

    [Test]
    public async Task ApiManagementService_WhenCreatedTwice_SecondCallSucceeds()
    {
        const string serviceName = "apim-upsert-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);
        var services = resourceGroup.GetApiManagementServices();

        await services.CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());
        var result = await services.CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        Assert.That(result.Value.Data.Name, Is.EqualTo(serviceName));
    }

    [Test]
    public async Task ApiManagementService_WhenRetrieved_ReturnsCorrectService()
    {
        const string serviceName = "apim-get-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        var result = await resourceGroup.GetApiManagementServiceAsync(serviceName);

        Assert.That(result.Value.Data.Name, Is.EqualTo(serviceName));
    }

    [Test]
    public async Task ApiManagementService_WhenNotFound_GetThrows404()
    {
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.GetApiManagementServiceAsync("nonexistent-apim"));
    }

    [Test]
    public async Task ApiManagementService_WhenUpdated_HasNewTags()
    {
        const string serviceName = "apim-update-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        var created = (await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData())).Value;

        var patch = new ApiManagementServicePatch();
        patch.Tags.Add("env", "updated");
        var result = await created.UpdateAsync(WaitUntil.Completed, patch);

        Assert.That(result.Value.Data.Tags["env"], Is.EqualTo("updated"));
    }

    [Test]
    public async Task ApiManagementService_WhenDeleted_CanNoLongerBeRetrieved()
    {
        const string serviceName = "apim-delete-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        var created = (await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData())).Value;

        await created.DeleteAsync(WaitUntil.Completed);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await resourceGroup.GetApiManagementServiceAsync(serviceName));
    }

    [Test]
    public async Task ApiManagementService_WhenDeleted_CanBeRetrievedAsDeletedService()
    {
        const string serviceName = "apim-get-deleted-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var created = (await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData())).Value;
        await created.DeleteAsync(WaitUntil.Completed);

        var deleted = (await subscription.GetApiManagementDeletedServiceAsync(AzureLocation.WestEurope, serviceName)).Value;

        Assert.That(deleted.Data.Name, Is.EqualTo(serviceName));
    }

    [Test]
    public async Task ApiManagementService_GetDeletedByName_WhenNotFound_Throws404()
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await subscription.GetApiManagementDeletedServiceAsync(AzureLocation.WestEurope, "nonexistent-apim"));
    }

    [Test]
    public async Task ApiManagementService_ListByResourceGroup_ContainsCreatedService()
    {
        const string serviceName = "apim-list-rg-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        var services = resourceGroup.GetApiManagementServices().GetAll().ToList();

        Assert.That(services.Any(s => s.Data.Name == serviceName), Is.True);
    }

    [Test]
    public async Task ApiManagementService_ListBySubscription_ContainsCreatedService()
    {
        const string serviceName = "apim-list-sub-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var services = subscription.GetApiManagementServicesAsync();
        var names = new List<string>();
        await foreach (var s in services)
            names.Add(s.Data.Name);

        Assert.That(names, Does.Contain(serviceName));
    }

    [Test]
    public async Task ApiManagementService_CheckNameAvailability_AvailableName_ReturnsAvailable()
    {
        var armClient = CreateArmClient();
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var result = await subscription.CheckApiManagementServiceNameAvailabilityAsync(
            new ApiManagementServiceNameAvailabilityContent("apim-available-xyz999"));

        Assert.That(result.Value.IsNameAvailable, Is.True);
    }

    [Test]
    public async Task ApiManagementService_CheckNameAvailability_TakenName_ReturnsNotAvailable()
    {
        const string serviceName = "apim-taken-name";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, serviceName, MinimalServiceData());

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var result = await subscription.CheckApiManagementServiceNameAvailabilityAsync(
            new ApiManagementServiceNameAvailabilityContent(serviceName));

        Assert.That(result.Value.IsNameAvailable, Is.False);
    }

    [Test]
    public async Task ApiManagementService_DeployedViaArmTemplate_ServiceAndChildResourcesExist()
    {
        const string serviceName = "apim-arm-deploy-test";
        var armClient = CreateArmClient();
        var resourceGroup = await GetResourceGroupAsync(armClient);

        await resourceGroup.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed,
            "deploy-apim-test",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/apim-test-resources.json")),
                Parameters = BinaryData.FromObjectAsJson(new
                {
                    serviceName = new { value = serviceName }
                })
            }));

        var service = (await resourceGroup.GetApiManagementServiceAsync(serviceName)).Value;
        var api = (await service.GetApiAsync("test-api")).Value;
        var backend = (await service.GetApiManagementBackendAsync("test-backend")).Value;
        var product = (await service.GetApiManagementProductAsync("test-product")).Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(service.Data.Name, Is.EqualTo(serviceName));
            Assert.That(api.Data.Path, Is.EqualTo("test-api"));
            Assert.That(backend.Data.Uri, Is.EqualTo(new Uri("https://backend.example.com")));
            Assert.That(product.Data.DisplayName, Is.EqualTo("Test Product"));
        }
    }
}
