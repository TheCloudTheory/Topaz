using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ApiManagement;
using Azure.ResourceManager.ApiManagement.Models;
using Topaz.CLI;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ApiManagementPolicyTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("A1B2C3D4-E5F6-4A5B-8C9D-AABBCC003700");
    private const string SubscriptionName = "sub-test-apim-policy";
    private const string ResourceGroupName = "rg-test-apim-policy";
    private const string ServiceName = "apim-policy-tests";
    private const string PolicyId = "policy";

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

    private static PolicyContractData MinimalPolicyData(string value = "<policies><inbound><base /></inbound><backend><base /></backend><outbound><base /></outbound></policies>") =>
        new() { Value = value, Format = PolicyContentFormat.Xml };

    private async Task<ApiManagementServiceResource> CreateServiceAsync(ArmClient armClient)
    {
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = (await subscription.GetResourceGroupAsync(ResourceGroupName)).Value;
        var result = await resourceGroup.GetApiManagementServices()
            .CreateOrUpdateAsync(WaitUntil.Completed, ServiceName, MinimalServiceData());
        return result.Value;
    }

    [Test]
    public async Task Policy_WhenCreated_HasCorrectProperties()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);
        const string policyValue = "<policies><inbound><base /></inbound><backend><base /></backend><outbound><base /></outbound></policies>";

        var result = await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData());

        Assert.That(result.Value, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value.Data.Name, Is.EqualTo(PolicyId));
            Assert.That(result.Value.Data.Value, Is.EqualTo(policyValue));
        }
    }

    [Test]
    public async Task Policy_WhenRetrieved_ReturnsCorrectPolicy()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData());

        var result = await service.GetApiManagementPolicyAsync(PolicyId);

        Assert.That(result.Value.Data.Name, Is.EqualTo(PolicyId));
    }

    [Test]
    public async Task Policy_WhenNotFound_GetThrows404()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementPolicyAsync(PolicyId));
    }

    [Test]
    public async Task Policy_WhenCreatedTwice_SecondCallRequiresIfMatch()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData());

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementPolicies()
                .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData()));

        Assert.That(ex!.Status, Is.EqualTo(400));
    }

    [Test]
    public async Task Policy_WhenUpdatedWithMatchingETag_Succeeds()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);
        const string updatedValue = "<policies><inbound><set-header name=\"X-Updated\" exists-action=\"override\"><value>1</value></set-header><base /></inbound><backend><base /></backend><outbound><base /></outbound></policies>";

        var created = (await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData())).Value;

        var etagResponse = await created.GetEntityTagAsync();
        var eTag = etagResponse.GetRawResponse().Headers.ETag.GetValueOrDefault();

        var updated = await created.UpdateAsync(WaitUntil.Completed, new PolicyContractData { Value = updatedValue, Format = PolicyContentFormat.Xml }, eTag);

        Assert.That(updated.Value.Data.Value, Is.EqualTo(updatedValue));
    }

    [Test]
    public async Task Policy_WhenUpdatedWithWrongETag_Throws409()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData())).Value;

        var ex = Assert.ThrowsAsync<RequestFailedException>(async () =>
            await created.UpdateAsync(WaitUntil.Completed, MinimalPolicyData(), new ETag("\"wrong-etag-value\"")));

        Assert.That(ex!.Status, Is.EqualTo(409));
    }

    [Test]
    public async Task Policy_WhenDeleted_IsNoLongerRetrievable()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var policy = (await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData())).Value;

        await policy.DeleteAsync(WaitUntil.Completed, ETag.All);

        Assert.ThrowsAsync<RequestFailedException>(async () =>
            await service.GetApiManagementPolicyAsync(PolicyId));
    }

    [Test]
    public async Task Policy_List_ContainsCreatedPolicy()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData());

        var policies = service.GetApiManagementPolicies().GetAll().Select(p => p.Data.Name).ToList();

        Assert.That(policies, Does.Contain(PolicyId));
    }

    [Test]
    public async Task Policy_GetEntityTag_ReturnsETag()
    {
        var armClient = CreateArmClient();
        var service = await CreateServiceAsync(armClient);

        var created = (await service.GetApiManagementPolicies()
            .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData())).Value;

        var response = await created.GetEntityTagAsync();

        Assert.That(response.GetRawResponse().Headers.ETag, Is.Not.Null);
    }

    [Test]
    public Task Policy_WhenParentServiceNotFound_CreateThrows404()
    {
        try
        {
            var armClient = CreateArmClient();

            var fakeService = armClient.GetApiManagementServiceResource(
                ApiManagementServiceResource.CreateResourceIdentifier(
                    SubscriptionId.ToString(), ResourceGroupName, "nonexistent-service"));

            Assert.ThrowsAsync<RequestFailedException>(async () =>
                await fakeService.GetApiManagementPolicies()
                    .CreateOrUpdateAsync(WaitUntil.Completed, PolicyId, MinimalPolicyData()));
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}
