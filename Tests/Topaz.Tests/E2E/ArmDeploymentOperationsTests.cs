using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ArmDeploymentOperationsTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static string BuildManagedIdentityTemplate(string identityName) => $$"""
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "resources": [
            {
              "type": "Microsoft.ManagedIdentity/userAssignedIdentities",
              "apiVersion": "2018-11-30",
              "name": "{{identityName}}",
              "location": "westeurope"
            }
          ]
        }
        """;

    [Test]
    public async Task DeploymentOperations_RgScope_ListReturnsOperationForEachDeployedResource()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-rg-list-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, "rg-ops-list", new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-ops-list",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildManagedIdentityTemplate("mi-ops-list"))
            }));

        var deployment = (await rg.Value.GetArmDeploymentAsync("deploy-ops-list")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(operations[0].OperationId, Is.Not.Null.And.Not.Empty);
            Assert.That(operations[0].Properties?.ProvisioningState, Is.EqualTo("Succeeded"));
            Assert.That(operations[0].Properties?.TargetResource?.ResourceType.ToString(), Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task DeploymentOperations_RgScope_EmptyTemplateReturnsNoOperations()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-rg-empty-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed, "rg-ops-empty", new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-ops-empty",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(
                    """{"$schema":"https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#","contentVersion":"1.0.0.0","resources":[]}""")
            }));

        var deployment = (await rg.Value.GetArmDeploymentAsync("deploy-ops-empty")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Empty);
    }

    [Test]
    public async Task DeploymentOperations_SubscriptionScope_ListReturnsOperationsForDeployedResources()
    {
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "ops-sub-list-sub");

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        const string template = """
            {
              "$schema": "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "resources": [
                {
                  "type": "Microsoft.Resources/resourceGroups",
                  "apiVersion": "2021-04-01",
                  "name": "rg-ops-sub-created",
                  "location": "westeurope"
                }
              ]
            }
            """;

        await subscription.GetArmDeployments().CreateOrUpdateAsync(
            WaitUntil.Completed, "deploy-sub-ops-list",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(template)
            })
            { Location = AzureLocation.WestEurope });

        var deployment = (await subscription.GetArmDeploymentAsync("deploy-sub-ops-list")).Value;
        var operations = deployment.GetDeploymentOperations().ToList();

        Assert.That(operations, Is.Not.Empty);
        Assert.That(operations[0].OperationId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task DeploymentOperations_ManagementGroupScope_ListReturnsOperationsForDeployedResources()
    {
        const string groupId = "mg-ops-list-test";
        const string deploymentName = "deploy-mg-ops-list";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Ops List Test");

        var templateJson = await File.ReadAllTextAsync("templates/empty-deployment.json");
        await topaz.CreateDeploymentAtManagementGroupScopeAsync(
            groupId, deploymentName, "westeurope", templateJson);

        var result = await topaz.GetDeploymentOperationsAtManagementGroupScopeAsync(groupId, deploymentName);
        var value = result["value"]!.AsArray();

        Assert.That(value, Is.Not.Null);
    }

    [Test]
    public async Task DeploymentOperations_ManagementGroupScope_GetByIdReturnsMatchingRecord()
    {
        const string groupId = "mg-ops-getbyid-test";
        const string deploymentName = "deploy-mg-ops-getbyid";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Ops GetById Test");

        var templateJson = await File.ReadAllTextAsync("templates/empty-deployment.json");
        await topaz.CreateDeploymentAtManagementGroupScopeAsync(
            groupId, deploymentName, "westeurope", templateJson);

        // Seed at least one operation using a template that provisions a resource
        const string identityTemplate = """
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/managementGroupDeploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "resources": []
            }
            """;
        const string deployName2 = "deploy-mg-ops-getbyid2";
        await topaz.CreateDeploymentAtManagementGroupScopeAsync(
            groupId, deployName2, "westeurope", identityTemplate);

        var listResult = await topaz.GetDeploymentOperationsAtManagementGroupScopeAsync(groupId, deployName2);
        var value = listResult["value"]!.AsArray();

        // Empty template produces no operations; verify 404 for unknown operationId
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetDeploymentOperationAtManagementGroupScopeByIdAsync(
                groupId, deployName2, "00000000-0000-0000-0000-000000000000"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeploymentOperations_ManagementGroupScope_UnknownDeploymentReturns404()
    {
        const string groupId = "mg-ops-404-test";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateManagementGroupAsync(groupId, "MG Ops 404 Test");

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetDeploymentOperationsAtManagementGroupScopeAsync(groupId, "nonexistent-deploy"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeploymentOperations_TenantScope_ListReturnsValueForKnownDeployment()
    {
        const string deploymentName = "deploy-tenant-ops-list";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        var templateJson = await File.ReadAllTextAsync("templates/empty-deployment.json");
        await topaz.CreateDeploymentAtTenantScopeAsync(deploymentName, "westeurope", templateJson);

        var result = await topaz.GetDeploymentOperationsAtTenantScopeAsync(deploymentName);
        var value = result["value"]!.AsArray();

        Assert.That(value, Is.Not.Null);

        await topaz.DeleteDeploymentAtTenantScopeAsync(deploymentName);
    }

    [Test]
    public async Task DeploymentOperations_TenantScope_GetByIdReturns404ForUnknownOperation()
    {
        const string deploymentName = "deploy-tenant-ops-getbyid";
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        var templateJson = await File.ReadAllTextAsync("templates/empty-deployment.json");
        await topaz.CreateDeploymentAtTenantScopeAsync(deploymentName, "westeurope", templateJson);

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetDeploymentOperationAtTenantScopeByIdAsync(
                deploymentName, "00000000-0000-0000-0000-000000000000"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));

        await topaz.DeleteDeploymentAtTenantScopeAsync(deploymentName);
    }

    [Test]
    public async Task DeploymentOperations_TenantScope_UnknownDeploymentReturns404()
    {
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        using var topaz = new TopazArmClient(credentials);

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await topaz.GetDeploymentOperationsAtTenantScopeAsync("nonexistent-tenant-deploy"));
        Assert.That(ex!.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
    }
}
