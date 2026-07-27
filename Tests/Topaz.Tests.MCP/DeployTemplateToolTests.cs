using NUnit.Framework;
using Topaz.MCP.Tools;

namespace Topaz.Tests.MCP;

[TestFixture]
public class DeployTemplateToolTests
{
    private const string EmptyTemplate = """
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "resources": []
        }
        """;

    [Test]
    public async Task DeployTemplateTool_CreateTenantDeployment_ReturnsName()
    {
        const string deploymentName = "mcp-tenant-deploy-name-test";

        var result = await DeployTemplateTool.CreateOrUpdateTenantDeployment(
            deploymentName, "westeurope", EmptyTemplate, McpTestFixture.ObjectId);

        Assert.That(result.Name, Is.EqualTo(deploymentName));
    }

    [Test]
    public async Task DeployTemplateTool_CreateTenantDeployment_ReturnsSucceededState()
    {
        const string deploymentName = "mcp-tenant-deploy-state-test";

        var result = await DeployTemplateTool.CreateOrUpdateTenantDeployment(
            deploymentName, "westeurope", EmptyTemplate, McpTestFixture.ObjectId);

        Assert.That(result.ProvisioningState, Is.EqualTo("Succeeded"));
    }

    [Test]
    public async Task DeployTemplateTool_DeleteTenantDeployment_Succeeds()
    {
        const string deploymentName = "mcp-tenant-deploy-delete-test";

        await DeployTemplateTool.CreateOrUpdateTenantDeployment(
            deploymentName, "westeurope", EmptyTemplate, McpTestFixture.ObjectId);

        Assert.DoesNotThrowAsync(async () =>
            await DeployTemplateTool.DeleteTenantDeployment(
                deploymentName, McpTestFixture.ObjectId));
    }
}
