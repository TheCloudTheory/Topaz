using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.KeyVault.Models;
using Azure.ResourceManager.ManagedServiceIdentities;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text.Json.Nodes;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ResourceManagerTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    
    [Test]
    public async Task ResourceManagerTest_WhenSubscriptionIsCreatedUsingArmClient_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        
        // Act
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(subscription.Data.Id.ToString(), Is.EqualTo($"/subscriptions/{subscriptionId}"));
            Assert.That(subscription.Data.SubscriptionId, Is.EqualTo(subscriptionId.ToString()));
            Assert.That(subscription.Data.DisplayName, Is.EqualTo(subscriptionName));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenDeploymentIsRequested_ItShouldBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment";
        
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment1.json"))
            }));
        var deployment = await rg.Value.GetArmDeploymentAsync(deploymentName);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deployment.Value.Data.Name, Is.EqualTo(deploymentName));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenDeploymentIsDeleted_ItShouldNotBeAvailable()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-to-delete";
        
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment1.json"))
            }));

        // Act
        await (await rg.Value.GetArmDeploymentAsync(deploymentName)).Value.DeleteAsync(WaitUntil.Completed);
        var deployments = rg.Value.GetArmDeployments().Where(deployment => deployment.Data.Name.Equals(deploymentName));
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(deployments.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenTemplateContainsSupportedResource_ItShouldBeDeployed()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-keyvault";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-keyvault.json"))
            }));
        
        // Assert
        var kv = await rg.Value.GetKeyVaultAsync("topaz-keyvault");

        Assert.Multiple(() =>
        {
            Assert.That(kv, Is.Not.Null);
            Assert.That(kv.Value.Data.Name, Is.EqualTo("topaz-keyvault"));
        });
        
        // Cleanup
        await kv.Value.DeleteAsync(WaitUntil.Completed);
        await topaz.PurgeKeyVault(subscriptionId, kv.Value.Data.Name, kv.Value.Data.Location);
    }
    
    [Test]
    public async Task ResourceManagerTest_WhenTemplateContainsParameters_TheyShouldBeSupported()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-parameters";
            
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-identity.json")),
                Parameters = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-identity.parameters.json"))
            }));
        
        // Assert
        var kv = await rg.Value.GetKeyVaultAsync("deploykeyvault01");
        var mi = await rg.Value.GetUserAssignedIdentityAsync("deployidentity01");

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(kv, Is.Not.Null);
                Assert.That(kv.Value.Data.Name, Is.EqualTo("deploykeyvault01"));
                Assert.That(mi, Is.Not.Null);
                Assert.That(mi.Value.Data.Name, Is.EqualTo("deployidentity01"));
                Assert.That(mi.Value.Data.Tags, Contains.Key("cost-center"));
                Assert.That(mi.Value.Data.Tags["cost-center"], Is.EqualTo("10008923"));
            });
        }
        finally
        {
            // Cleanup
            await kv.Value.DeleteAsync(WaitUntil.Completed);
            await topaz.PurgeKeyVault(subscriptionId, kv.Value.Data.Name, kv.Value.Data.Location);
        }
    }

    [Test]
    public async Task ResourceManagerTest_WhenTemplateContainsContainerRegistry_ItShouldBeDeployed()
    {
        // Arrange
        const string subscriptionName = "test-sub";
        const string resourceGroupName = "rg-deployment";
        const string deploymentName = "deployment-acr";
        const string registryName = "topazacrdeploy01";

        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, subscriptionName);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, resourceGroupName,
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, deploymentName,
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(await File.ReadAllTextAsync("templates/deployment-acr.json"))
            }));

        // Assert
        var registry = await rg.Value.GetContainerRegistryAsync(registryName);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(registry, Is.Not.Null);
                Assert.That(registry.Value.Data.Name, Is.EqualTo(registryName));
                Assert.That(registry.Value.Data.Sku.Name, Is.EqualTo(ContainerRegistrySkuName.Basic));
                Assert.That(registry.Value.Data.LoginServer, Is.Not.Null.And.Not.Empty);
            });
        }
        finally
        {
            await registry.Value.DeleteAsync(WaitUntil.Completed);
        }
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateIsCalledOnEmptyGroup_ItShouldReturnEmptyResourcesList()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-empty",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-empty");

        // Assert
        var template = result["template"]!;
        Assert.Multiple(() =>
        {
            Assert.That(template["$schema"]!.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            Assert.That(template["contentVersion"]!.GetValue<string>(), Is.EqualTo("1.0.0.0"));
            Assert.That(template["resources"]!.AsArray(), Is.Empty);
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateIsCalledOnGroupWithManagedIdentity_ItShouldParameterizeNameAndLocation()
    {
        // Arrange
        const string miName = "mi-export-default";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-default",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-default");

        // Assert – name and location should both be parameterized by default
        var template = result["template"]!;
        var resources = template["resources"]!.AsArray();
        var parameters = template["parameters"]!.AsObject();

        Assert.Multiple(() =>
        {
            Assert.That(resources, Has.Count.EqualTo(1));
            Assert.That(resources[0]!["name"]!.GetValue<string>(), Does.StartWith("[parameters("));
            Assert.That(resources[0]!["location"]!.GetValue<string>(), Does.StartWith("[parameters("));
            Assert.That(parameters.Count, Is.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithIncludeParameterDefaultValue_ItShouldHaveDefaultValues()
    {
        // Arrange
        const string miName = "mi-export-defaultval";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-defaultval",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-defaultval", "IncludeParameterDefaultValue");

        // Assert – each parameter should have a defaultValue
        var parameters = result["template"]!["parameters"]!.AsObject();
        Assert.That(parameters, Is.Not.Empty);
        foreach (var (_, paramValue) in parameters)
        {
            Assert.That(paramValue!["defaultValue"], Is.Not.Null,
                $"Parameter should have a defaultValue when IncludeParameterDefaultValue is set");
        }
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithIncludeComments_ItShouldHaveMetadataDescription()
    {
        // Arrange
        const string miName = "mi-export-comments";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-comments",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-comments", "IncludeComments");

        // Assert – each parameter should have metadata.description
        var parameters = result["template"]!["parameters"]!.AsObject();
        Assert.That(parameters, Is.Not.Empty);
        foreach (var (_, paramValue) in parameters)
        {
            Assert.That(paramValue!["metadata"]?["description"], Is.Not.Null,
                "Parameter should have metadata.description when IncludeComments is set");
        }
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithSkipResourceNameParameterization_ItShouldHaveLiteralName()
    {
        // Arrange
        const string miName = "mi-export-skipname";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-skipname",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-skipname", "SkipResourceNameParameterization");

        // Assert – name is literal, location is still parameterized
        var template = result["template"]!;
        var resource = template["resources"]!.AsArray()[0]!;
        Assert.Multiple(() =>
        {
            Assert.That(resource["name"]!.GetValue<string>(), Is.EqualTo(miName));
            Assert.That(resource["location"]!.GetValue<string>(), Does.StartWith("[parameters("));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithSkipAllParameterization_ItShouldHaveNoParameters()
    {
        // Arrange
        const string miName = "mi-export-skipall";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-skipall",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-skipall", "SkipAllParameterization");

        // Assert – parameters is empty, name and location are literal values
        var template = result["template"]!;
        var parameters = template["parameters"]!.AsObject();
        var resource = template["resources"]!.AsArray()[0]!;
        Assert.Multiple(() =>
        {
            Assert.That(parameters, Is.Empty);
            Assert.That(resource["name"]!.GetValue<string>(), Is.EqualTo(miName));
            Assert.That(resource["location"]!.GetValue<string>(), Does.Not.StartWith("[parameters("));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateOnGroupWithMultipleResources_ItShouldIncludeAll()
    {
        // Arrange
        const string miName = "mi-export-multi";
        const string kvName = "kv-export-multi01";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-multi",
            new ResourceGroupData(AzureLocation.WestEurope));

        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        await rg.Value.GetKeyVaults().CreateOrUpdateAsync(WaitUntil.Completed, kvName,
            new KeyVaultCreateOrUpdateContent(AzureLocation.WestEurope,
                new KeyVaultProperties(Guid.Empty, new KeyVaultSku(KeyVaultSkuFamily.A, KeyVaultSkuName.Standard))));

        try
        {
            // Act
            var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-multi");

            // Assert – both managed identity and key vault should appear in the template
            var resources = result["template"]!["resources"]!.AsArray();
            Assert.That(resources, Has.Count.EqualTo(2));
        }
        finally
        {
            var kv = await rg.Value.GetKeyVaultAsync(kvName);
            await kv.Value.DeleteAsync(WaitUntil.Completed);
            await topaz.PurgeKeyVault(subscriptionId, kvName, kv.Value.Data.Location);
        }
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithWildcard_ItShouldExportAllResources()
    {
        // Arrange
        const string miName = "mi-export-wildcard";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-wildcard",
            new ResourceGroupData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        // Act – passing "*" should behave identically to passing no filter
        var resultWildcard = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-wildcard", resources: ["*"]);
        var resultDefault = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-wildcard");

        // Assert – both calls return the same number of resources
        var wildcardResources = resultWildcard["template"]!["resources"]!.AsArray();
        var defaultResources = resultDefault["template"]!["resources"]!.AsArray();
        Assert.Multiple(() =>
        {
            Assert.That(wildcardResources, Has.Count.EqualTo(1));
            Assert.That(wildcardResources.Count, Is.EqualTo(defaultResources.Count));
        });
    }

    [Test]
    public async Task ResourceManagerTest_WhenExportTemplateWithSpecificResourceId_ItShouldExportOnlyThatResource()
    {
        // Arrange – create two resources, request only one by ID
        const string miName1 = "mi-export-specific1";
        const string miName2 = "mi-export-specific2";
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "test-sub");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-export-specific",
            new ResourceGroupData(AzureLocation.WestEurope));

        var mi1 = await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName1,
            new UserAssignedIdentityData(AzureLocation.WestEurope));
        await rg.Value.GetUserAssignedIdentities().CreateOrUpdateAsync(WaitUntil.Completed, miName2,
            new UserAssignedIdentityData(AzureLocation.WestEurope));

        var mi1Id = mi1.Value.Data.Id.ToString();

        // Act – request only mi1 by its resource ID
        var result = await topaz.ExportTemplateAsync(subscriptionId, "rg-export-specific", resources: [mi1Id]);

        // Assert – only the requested resource appears in the template
        var resources = result["template"]!["resources"]!.AsArray();
        Assert.That(resources, Has.Count.EqualTo(1));
    }
}