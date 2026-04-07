namespace Topaz.Tests.AzureCLI;

public class ContainerRegistryTests : TopazFixture
{
    [Test]
    public async Task ContainerRegistry_Create_Show_And_Delete()
    {
        const string registryName = "topazacr01";
        const string resourceGroup = "test-acr-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");

        // create
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(registryName));
                Assert.That(resp["sku"]!["name"]!.GetValue<string>(), Is.EqualTo("Basic"));
                Assert.That(resp["loginServer"], Is.Not.Null);
                Assert.That(resp["provisioningState"]!.GetValue<string>(), Is.EqualTo("Succeeded"));
            });
        });

        // show
        await RunAzureCliCommand($"az acr show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(registryName));
        });

        // delete
        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");

        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_List_ByResourceGroup()
    {
        const string registryName = "topazacr02";
        const string resourceGroup = "test-acr-list-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");

        await RunAzureCliCommand($"az acr list --resource-group {resourceGroup}", (resp) =>
        {
            var arr = resp.AsArray();
            Assert.That(arr.Any(r => r!["name"]!.GetValue<string>() == registryName), Is.True);
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_Update_AdminUser_Enabled()
    {
        const string registryName = "topazacr03";
        const string resourceGroup = "test-acr-update-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Premium --location westeurope");

        await RunAzureCliCommand($"az acr update --name {registryName} --resource-group {resourceGroup} --admin-enabled true", (resp) =>
        {
            Assert.That(resp["adminUserEnabled"]!.GetValue<bool>(), Is.True);
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_CheckName_Available()
    {
        await RunAzureCliCommand("az acr check-name -n uniqueacrname01", (resp) =>
        {
            Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.True);
        });
    }

    [Test]
    public async Task ContainerRegistry_CheckName_AlreadyExists()
    {
        const string registryName = "topazacr04";
        const string resourceGroup = "test-acr-check-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope");

        await RunAzureCliCommand($"az acr check-name -n {registryName}", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.False);
                Assert.That(resp["reason"]!.GetValue<string>(), Is.EqualTo("AlreadyExists"));
            });
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_CheckName_Invalid()
    {
        await RunAzureCliCommand("az acr check-name -n ab", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["nameAvailable"]!.GetValue<bool>(), Is.False);
                Assert.That(resp["reason"]!.GetValue<string>(), Is.EqualTo("Invalid"));
            });
        });
    }

    [Test]
    public async Task ContainerRegistry_Create_WithNonExistingResourceGroup_ShouldFail()
    {
        await RunAzureCliCommand("az acr create --name topazacr05 --resource-group non-existing-rg --sku Basic --location westeurope", null, 1);
    }

    [Test]
    public async Task ContainerRegistry_Login_ShouldAuthenticateViaAcrAndDocker()
    {
        const string registryName = "topazacr06";
        const string resourceGroup = "test-acr-login-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        // Retrieve admin credentials so we can use them for docker login.
        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        // az acr login: --expose-token returns an exchange token without requiring a local Docker daemon.
        await RunAzureCliCommand($"az acr login --name {registryName} --expose-token", (resp) =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(resp["accessToken"], Is.Not.Null);
                Assert.That(resp["loginServer"], Is.Not.Null);
            });
        });

        // docker login: authenticate against the Docker Registry V2 API using Basic Auth,
        // replicating what `docker login <loginServer>` does internally.
        // Uses ACR admin credentials (username = registry name, password from credential show).
        await RunAzureCliCommand(
            $"curl -skf -u \"{registryName}:{adminPassword}\" " +
            $"https://{registryName}.cr.topaz.local.dev:8892/v2/");

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_ShowUsage_ShouldReturnSizeAndWebhookQuotas()
    {
        const string registryName = "topazacr07";
        const string resourceGroup = "test-acr-usage-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand($"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");

        await RunAzureCliCommand($"az acr show-usage --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            var values = resp["value"]!.AsArray();
            var size = values.FirstOrDefault(v => v!["name"]!.GetValue<string>() == "Size");
            var webhooks = values.FirstOrDefault(v => v!["name"]!.GetValue<string>() == "Webhooks");

            Assert.Multiple(() =>
            {
                Assert.That(size, Is.Not.Null);
                Assert.That(size!["limit"]!.GetValue<long>(), Is.EqualTo(107374182400L));
                Assert.That(size["currentValue"]!.GetValue<long>(), Is.EqualTo(0));
                Assert.That(webhooks, Is.Not.Null);
                Assert.That(webhooks!["limit"]!.GetValue<long>(), Is.EqualTo(10L));
            });
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_ListRepositories_ShouldReturnPushedRepository()
    {
        const string registryName = "topazacrrepolist01";
        const string resourceGroup = "test-acr-repolist-rg";
        const string repoName = "my-app";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        // Retrieve admin credentials so we can push directly via curl.
        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        // Push a minimal manifest to create a repository on the data plane.
        var loginServer = $"{registryName}.cr.topaz.local.dev:8892";
        const string manifestJson =
            "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
            "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
            "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
            "\"layers\":[]}";

        await RunAzureCliCommand(
            $"curl -skf -u \"{registryName}:{adminPassword}\" " +
            $"-X PUT https://{loginServer}/v2/{repoName}/manifests/v1 " +
            $"-H \"Content-Type: application/vnd.docker.distribution.manifest.v2+json\" " +
            $"-d '{manifestJson}'");

        // az acr repository list calls GET /v2/_catalog on the data plane.
        await RunAzureCliCommand($"az acr repository list --name {registryName}", (resp) =>
        {
            var repos = resp.AsArray().Select(r => r!.GetValue<string>()).ToList();
            Assert.That(repos, Contains.Item(repoName));
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }
}
