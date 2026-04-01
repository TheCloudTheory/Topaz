namespace Topaz.Tests.AzureCLI;

public class ResourceManagerTests : TopazFixture
{
    [Test]
    public async Task ResourceManagerTests_WhenDeploymentIsCreated_ItShouldBePossibleToGetIt()
    {
        await RunAzureCliCommand("az group create -n rg-deployment -l westeurope");
        await RunAzureCliCommand("az deployment group create --name test-deployment -g rg-deployment --template-file \"/templates/empty-deployment.json\"");
        await RunAzureCliCommand("az deployment group show --name test-deployment -g rg-deployment", response =>
        {
            Assert.That(response["name"]!.GetValue<string>(), Is.EqualTo("test-deployment"));
        });
        await RunAzureCliCommand("az group delete -n rg-deployment --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateIsRequested_ItShouldReturnTemplateWithSchema()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli -l westeurope");
        await RunAzureCliCommand("az group export -n rg-export-cli", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            Assert.That(response["contentVersion"]?.GetValue<string>(), Is.EqualTo("1.0.0.0"));
            Assert.That(response["resources"], Is.Not.Null);
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateWithIncludeComments_ItShouldHaveParameterMetadata()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli-comments -l westeurope");
        await RunAzureCliCommand("az identity create -n mi-cli-comments -g rg-export-cli-comments -l westeurope");
        await RunAzureCliCommand("az group export -n rg-export-cli-comments --include-comments", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            var parameters = response["parameters"]?.AsObject();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var (_, paramValue) in parameters)
                {
                    Assert.That(paramValue!["metadata"]?["description"], Is.Not.Null,
                        "Parameter should have metadata.description when --include-comments is set");
                }
            }
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli-comments --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateWithSkipAllParameterization_ItShouldHaveNoParameters()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli-skipall -l westeurope");
        await RunAzureCliCommand("az identity create -n mi-cli-skipall -g rg-export-cli-skipall -l westeurope");
        await RunAzureCliCommand("az group export -n rg-export-cli-skipall --skip-all-params", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            var parameters = response["parameters"]?.AsObject();
            Assert.That(parameters, Is.Null.Or.Empty,
                "Parameters should be empty when --skip-all-params is set");
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli-skipall --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateWithIncludeParameterDefaultValue_ItShouldHaveDefaultValues()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli-defaults -l westeurope");
        await RunAzureCliCommand("az identity create -n mi-cli-defaults -g rg-export-cli-defaults -l westeurope");
        await RunAzureCliCommand("az group export -n rg-export-cli-defaults --include-parameter-default-value", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            var parameters = response["parameters"]?.AsObject();
            if (parameters != null && parameters.Count > 0)
            {
                foreach (var (_, paramValue) in parameters)
                {
                    Assert.That(paramValue!["defaultValue"], Is.Not.Null,
                        "Parameter should have defaultValue when --include-parameter-default-value is set");
                }
            }
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli-defaults --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateWithSkipResourceNameParameterization_ItShouldHaveLiteralName()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli-skipname -l westeurope");
        await RunAzureCliCommand("az identity create -n mi-cli-skipname -g rg-export-cli-skipname -l westeurope");
        await RunAzureCliCommand("az group export -n rg-export-cli-skipname --skip-all-params", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            var resources = response["resources"]?.AsArray();
            if (resources != null && resources.Count > 0)
            {
                Assert.That(resources[0]!["name"]?.GetValue<string>(), Is.EqualTo("mi-cli-skipname"),
                    "Resource name should be a literal string when --skip-all-params is set");
            }
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli-skipname --yes");
    }

    [Test]
    public async Task ResourceManagerTests_WhenExportTemplateWithWildcardResource_ItShouldExportAllResources()
    {
        await RunAzureCliCommand("az group create -n rg-export-cli-wildcard -l westeurope");
        await RunAzureCliCommand("az identity create -n mi-cli-wildcard -g rg-export-cli-wildcard -l westeurope");
        // Azure CLI passes "*" via --resource-ids when exporting all resources
        await RunAzureCliCommand("az group export -n rg-export-cli-wildcard --resource-ids \"*\"", response =>
        {
            Assert.That(response["$schema"]?.GetValue<string>(), Does.Contain("deploymentTemplate.json"));
            var resources = response["resources"]?.AsArray();
            Assert.That(resources, Is.Not.Null.And.Not.Empty,
                "All resources should be exported when --resource-ids \"*\" is passed");
        });
        await RunAzureCliCommand("az group delete -n rg-export-cli-wildcard --yes");
    }
}