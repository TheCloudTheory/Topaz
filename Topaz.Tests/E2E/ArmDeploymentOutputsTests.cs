using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using System.Text;
using Topaz.Identity;
using Topaz.ResourceManager;

namespace Topaz.Tests.E2E;

public class ArmDeploymentOutputsTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;

    private static string BuildTemplateWithLiteralOutput() => $$"""
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "parameters": {},
          "variables": {},
          "resources": [],
          "outputs": {
            "staticValue": {
              "type": "string",
              "value": "hello-world"
            }
          }
        }
        """;

    private static string BuildTemplateWithParameterOutput() => $$"""
        {
          "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
          "contentVersion": "1.0.0.0",
          "parameters": {
            "inputValue": {
              "type": "string",
              "defaultValue": "test-parameter"
            }
          },
          "variables": {},
          "resources": [],
          "outputs": {
            "parameterOutput": {
              "type": "string",
              "value": "[parameters('inputValue')]"
            }
          }
        }
        """;

    [Test]
    public async Task ArmDeployment_WithLiteralOutput_ReturnsOutputValue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-outputs-literal");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-outputs-literal",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "deploy-literal-output",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildTemplateWithLiteralOutput())
            }));

        // Get deployment to check outputs
        var deployment = await rg.Value.GetArmDeploymentAsync("deploy-literal-output");

        // Assert
        Assert.That(deployment.Value.Data.Properties, Is.Not.Null);
        Assert.That(deployment.Value.Data.Properties.Outputs, Is.Not.Null, "Expected deployment outputs to be populated.");
        
        // Decode the BinaryData to get the JSON string
        var outputsData = deployment.Value.Data.Properties.Outputs;
        var outputsJson = Encoding.UTF8.GetString(outputsData!.ToMemory().ToArray());
        
        Assert.That(outputsJson, Does.Contain("staticValue"), "Expected 'staticValue' output in deployment outputs.");
        Assert.That(outputsJson, Does.Contain("hello-world"), "Expected 'hello-world' value in deployment outputs.");
    }

    [Test]
    public async Task ArmDeployment_WithParameterOutput_ReturnsEvaluatedValue()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var credentials = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credentials, subscriptionId.ToString(), ArmClientOptions);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(subscriptionId, "arm-outputs-param");
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var rg = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, "rg-outputs-param",
            new ResourceGroupData(AzureLocation.WestEurope));

        // Act — template uses parameters() function in output, which should be evaluated
        await rg.Value.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, "deploy-param-output",
            new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(BuildTemplateWithParameterOutput())
            }));

        // Get deployment to check outputs
        var deployment = await rg.Value.GetArmDeploymentAsync("deploy-param-output");

        // Assert
        Assert.That(deployment.Value.Data.Properties, Is.Not.Null);
        Assert.That(deployment.Value.Data.Properties.Outputs, Is.Not.Null, "Expected deployment outputs to be populated.");
        
        // Decode the BinaryData to get the JSON string
        var outputsData = deployment.Value.Data.Properties.Outputs;
        var outputsJson = Encoding.UTF8.GetString(outputsData!.ToMemory().ToArray());
        
        Assert.That(outputsJson, Does.Contain("parameterOutput"), "Expected 'parameterOutput' output in deployment outputs.");
        Assert.That(outputsJson, Does.Contain("test-parameter"), "Expected evaluated parameter value in deployment outputs.");
    }
}
