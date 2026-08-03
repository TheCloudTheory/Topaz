using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ApiManagementProductTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003500");
    private const string SubscriptionName = "sub-test-apim-product";
    private const string ResourceGroupName = "rg-test-apim-product";
    private const string ServiceName = "apim-product-tests";

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

    private static ApiManagementProductData MinimalProductData(string displayName = "Test Product") =>
        new() { DisplayName = displayName };

    private async Task<ApiManagementServiceResource> CreateServiceAsync(ArmClient armClient)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;
        var result = await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, ServiceName, MinimalServiceData());
        return result.Value;
    }

    [Test]
    public async Task Product_WhenCreated_HasCorrectProperties()
    {
        const string productId = "product-create-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var result = await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData("Created Product"));

        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(productId));
            Assert.That(result.Value.Data.DisplayName, Is.EqualTo("Created Product"));
        }
    }

    [Test]
    public async Task Product_WhenRetrieved_ReturnsCorrectProduct()
    {
        const string productId = "product-get-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData("Get Product"));

        var result = await service.GetApiManagementProductAsync(productId);

        Assert.That(result.Value.Data.Name, Is.EqualTo(productId));
    }

    [Test]
    public async Task Product_WhenNotFound_GetThrows404()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementProductAsync("nonexistent-product"));
    }

    [Test]
    public async Task Product_WhenUpdated_ReflectsNewDisplayName()
    {
        const string productId = "product-update-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData("Original Name"))).Value;

        var updated = await created.UpdateAsync(ETag.All, new ApiManagementProductPatch
        {
            DisplayName = "Updated Name"
        });

        Assert.That(updated.Value.Data.DisplayName, Is.EqualTo("Updated Name"));
    }

    [Test]
    public async Task Product_WhenUpdatedWithMatchingETag_Succeeds()
    {
        const string productId = "product-update-etag";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData("Etag Product"))).Value;

        var etagResponse = await created.GetEntityTagAsync();
        var eTag = etagResponse.GetRawResponse().Headers.ETag.GetValueOrDefault();
        var updated = await created.UpdateAsync(eTag, new ApiManagementProductPatch { DisplayName = "Etag Updated" });

        Assert.That(updated.Value.Data.DisplayName, Is.EqualTo("Etag Updated"));
    }

    [Test]
    public async Task Product_WhenUpdatedWithWrongETag_Throws409()
    {
        const string productId = "product-update-wrong-etag";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData("Wrong Etag Product"))).Value;

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await created.UpdateAsync(new ETag("\"wrong-etag-value\""), new ApiManagementProductPatch { DisplayName = "Should Fail" }));

        Assert.That(ex!.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Product_WhenDeleted_IsNoLongerRetrievable()
    {
        const string productId = "product-delete-test";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var product = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData())).Value;

        await product.DeleteAsync(WaitUntil.Completed, ETag.All);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementProductAsync(productId));
    }

    [Test]
    public async Task Product_List_ContainsCreatedProducts()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementProducts().CreateOrUpdateAsync(WaitUntil.Completed, "product-list-1", MinimalProductData("List Product 1"));
        await service.GetApiManagementProducts().CreateOrUpdateAsync(WaitUntil.Completed, "product-list-2", MinimalProductData("List Product 2"));

        var products = service.GetApiManagementProducts().GetAll().Select(p => p.Data.Name).ToList();

        Assert.That(products, Does.Contain("product-list-1"));
        Assert.That(products, Does.Contain("product-list-2"));
    }

    [Test]
    public async Task Product_WhenParentServiceNotFound_CreateThrows404()
    {
        var armClient = CreateArmClient();

        var fakeService = armClient.GetApiManagementServiceResource(
            ApiManagementServiceResource.CreateResourceIdentifier(
                SubscriptionId.ToString(), ResourceGroupName, "nonexistent-service"));

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await fakeService.GetApiManagementProducts()
                .CreateOrUpdateAsync(WaitUntil.Completed, "any-product", MinimalProductData()));
    }

    [Test]
    public async Task ProductApi_WhenAssigned_IsInProductApiList()
    {
        const string productId = "product-api-assign";
        const string apiId = "api-for-product";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var product = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData())).Value;
        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, apiId,
            new ApiCreateOrUpdateContent { Path = "product-api-path", DisplayName = "Product API" });

        await product.CreateOrUpdateProductApiAsync(apiId);

        var apis = product.GetProductApis().ToList();
        Assert.That(apis.Select(a => a.Name), Does.Contain(apiId));
    }

    [Test]
    public async Task ProductApi_WhenAssignmentChecked_ReturnsTrue()
    {
        const string productId = "product-api-check";
        const string apiId = "api-for-check";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var product = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData())).Value;
        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, apiId,
            new ApiCreateOrUpdateContent { Path = "check-api-path", DisplayName = "Check API" });

        await product.CreateOrUpdateProductApiAsync(apiId);

        var exists = await product.CheckProductApiEntityExistsAsync(apiId);
        Assert.That(exists.Value, Is.True);
    }

    [Test]
    public async Task ProductApi_WhenAssignmentChecked_AndNotAssigned_ReturnsFalse()
    {
        const string productId = "product-api-check-false";
        const string apiId = "api-not-assigned";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var product = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData())).Value;
        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, apiId,
            new ApiCreateOrUpdateContent { Path = "unassigned-api-path", DisplayName = "Unassigned API" });

        var exists = await product.CheckProductApiEntityExistsAsync(apiId);
        Assert.That(exists.Value, Is.False);
    }

    [Test]
    public async Task ProductApi_WhenDeleted_IsNoLongerInProductApiList()
    {
        const string productId = "product-api-delete";
        const string apiId = "api-for-delete";
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var product = (await service.GetApiManagementProducts()
            .CreateOrUpdateAsync(WaitUntil.Completed, productId, MinimalProductData())).Value;
        await service.GetApis().CreateOrUpdateAsync(WaitUntil.Completed, apiId,
            new ApiCreateOrUpdateContent { Path = "delete-api-path", DisplayName = "Delete API" });

        await product.CreateOrUpdateProductApiAsync(apiId);
        await product.DeleteProductApiAsync(apiId);

        var apis = product.GetProductApis().ToList();
        Assert.That(apis.Select(a => a.Name), Does.Not.Contain(apiId));
    }
}
