using Topaz.Shared;

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
            $"https://{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}/v2/");

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
        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
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

    [Test]
    public async Task ContainerRegistry_ShowTags_ShouldReturnPushedTags()
    {
        const string registryName = "topazacrtaglist01";
        const string resourceGroup = "test-acr-taglist-rg";
        const string repoName = "tag-list-app";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        // Retrieve admin credentials so we can push directly via curl.
        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
        const string manifestJson =
            "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
            "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
            "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
            "\"layers\":[]}";

        // Push three tags to the repository.
        foreach (var tag in new[] { "v1", "v2", "latest" })
        {
            await RunAzureCliCommand(
                $"curl -skf -u \"{registryName}:{adminPassword}\" " +
                $"-X PUT https://{loginServer}/v2/{repoName}/manifests/{tag} " +
                $"-H \"Content-Type: application/vnd.docker.distribution.manifest.v2+json\" " +
                $"-d '{manifestJson}'");
        }

        // az acr repository show-tags calls GET /v2/{name}/tags/list on the data plane.
        await RunAzureCliCommand($"az acr repository show-tags --name {registryName} --repository {repoName}", (resp) =>
        {
            var tags = resp.AsArray().Select(t => t!.GetValue<string>()).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(tags, Contains.Item("v1"));
                Assert.That(tags, Contains.Item("v2"));
                Assert.That(tags, Contains.Item("latest"));
            });
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_HeadManifest_ExistingTag_ShouldReturn200()
    {
        const string registryName = "topazacrheadmanifest01";
        const string resourceGroup = "test-acr-headmanifest-rg";
        const string repoName = "head-manifest-app";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
        const string manifestJson =
            "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
            "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
            "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
            "\"layers\":[]}";

        // Push a manifest under tag "v1"
        await RunAzureCliCommand(
            $"curl -skf -u \"{registryName}:{adminPassword}\" " +
            $"-X PUT https://{loginServer}/v2/{repoName}/manifests/v1 " +
            $"-H \"Content-Type: application/vnd.docker.distribution.manifest.v2+json\" " +
            $"-d '{manifestJson}'");

        // HEAD should succeed (exit 0) and return 200 - curl -f fails on 4xx/5xx
        await RunAzureCliCommand(
            $"curl -skf -u \"{registryName}:{adminPassword}\" " +
            $"-I https://{loginServer}/v2/{repoName}/manifests/v1");

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_HeadManifest_NotFound_ShouldReturn404()
    {
        const string registryName = "topazacrheadmanifest02";
        const string resourceGroup = "test-acr-headmanifest404-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";

        // HEAD on a non-existent manifest.
        // With curl -f, HTTP 404 maps to exit code 22 (HTTP page not retrieved).
        await RunAzureCliCommand(
            $"curl -skf -u \"{registryName}:{adminPassword}\" " +
            $"-I https://{loginServer}/v2/nonexistent-repo/manifests/v1",
            null, 22);

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_RepositoryDelete_Image_ShouldDeleteOnlySpecifiedTag()
    {
        const string registryName = "topazacrimgdel01";
        const string resourceGroup = "test-acr-repo-del-image-rg";
        const string repoName = "repo-delete-image-app";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
        var manifestsByTag = new Dictionary<string, string>
        {
            ["v1"] =
                "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
                "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
                "\"digest\":\"sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
                "\"layers\":[]}",
            ["v2"] =
                "{\"schemaVersion\":2,\"mediaType\":\"application/vnd.docker.distribution.manifest.v2+json\"," +
                "\"config\":{\"mediaType\":\"application/vnd.docker.container.image.v1+json\",\"size\":0," +
                "\"digest\":\"sha256:f3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\"}," +
                "\"layers\":[]}"
        };

        foreach (var (tag, manifestJson) in manifestsByTag)
        {
            await RunAzureCliCommand(
                $"curl -skf -u \"{registryName}:{adminPassword}\" " +
                $"-X PUT https://{loginServer}/v2/{repoName}/manifests/{tag} " +
                $"-H \"Content-Type: application/vnd.docker.distribution.manifest.v2+json\" " +
                $"-d '{manifestJson}'");
        }

        await RunAzureCliCommand(
            $"az acr repository delete --name {registryName} --image {repoName}:v1 --yes");

        await RunAzureCliCommand($"az acr repository show-tags --name {registryName} --repository {repoName}", (resp) =>
        {
            var tags = resp.AsArray().Select(t => t!.GetValue<string>()).ToList();
            Assert.Multiple(() =>
            {
                Assert.That(tags, Does.Not.Contain("v1"));
                Assert.That(tags, Contains.Item("v2"));
            });
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task ContainerRegistry_RepositoryDelete_Repository_ShouldDeleteRepository()
    {
        const string registryName = "topazacrrepodel01";
        const string resourceGroup = "test-acr-repo-del-rg";
        const string repoName = "repo-delete-all-app";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Basic --location westeurope --admin-enabled true");

        string adminPassword = string.Empty;
        await RunAzureCliCommand($"az acr credential show --name {registryName} --resource-group {resourceGroup}", (resp) =>
        {
            adminPassword = resp["passwords"]!.AsArray()[0]!["value"]!.GetValue<string>();
        });

        var loginServer = $"{registryName}.cr.topaz.local.dev:{GlobalSettings.ContainerRegistryPort}";
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

        await RunAzureCliCommand(
            $"az acr repository delete --name {registryName} --repository {repoName} --yes");

        await RunAzureCliCommand($"az acr repository list --name {registryName}", (resp) =>
        {
            var repos = resp.AsArray().Select(r => r!.GetValue<string>()).ToList();
            Assert.That(repos, Does.Not.Contain(repoName));
        });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTask_CreateShowListDelete_ShouldSucceed()
    {
        const string registryName = "topazacrtask01";
        const string resourceGroup = "test-acr-task-rg";
        const string taskName = "my-build-task";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");

        await RunAzureCliCommand(
            $"az acr task create --name {taskName} --registry {registryName} " +
            $"--resource-group {resourceGroup} --cmd \"echo hello\" --no-push --context /dev/null");

        await RunAzureCliCommand(
            $"az acr task show --name {taskName} --registry {registryName} --resource-group {resourceGroup}",
            resp =>
            {
                Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(taskName));
            });

        await RunAzureCliCommand(
            $"az acr task list --registry {registryName} --resource-group {resourceGroup}",
            resp =>
            {
                var names = resp.AsArray().Select(t => t!["name"]!.GetValue<string>()).ToList();
                Assert.That(names, Does.Contain(taskName));
            });

        await RunAzureCliCommand(
            $"az acr task delete --name {taskName} --registry {registryName} --resource-group {resourceGroup} --yes");

        await RunAzureCliCommand(
            $"az acr task list --registry {registryName} --resource-group {resourceGroup}",
            resp =>
            {
                var names = resp.AsArray().Select(t => t!["name"]!.GetValue<string>()).ToList();
                Assert.That(names, Does.Not.Contain(taskName));
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_TriggerAndListRuns_ShouldSucceed()
    {
        const string registryName = "topazacrrun01";
        const string resourceGroup = "test-acr-run-rg";
        const string taskName = "run-task";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");
        await RunAzureCliCommand(
            $"az acr task create --name {taskName} --registry {registryName} " +
            $"--resource-group {resourceGroup} --cmd \"echo hello\" --no-push --context /dev/null");

        string? runId = null;
        await RunAzureCliCommand(
            $"az acr task run --name {taskName} --registry {registryName} --resource-group {resourceGroup} --no-logs",
            resp =>
            {
                runId = resp["runId"]?.GetValue<string>();
                Assert.That(runId, Is.Not.Null.And.Not.Empty);
                Assert.That(resp["status"]?.GetValue<string>(), Is.EqualTo("Succeeded"));
            });

        await RunAzureCliCommand(
            $"az acr task list-runs --registry {registryName} --resource-group {resourceGroup}",
            resp =>
            {
                var ids = resp.AsArray().Select(r => r!["runId"]?.GetValue<string>()).ToList();
                Assert.That(ids, Does.Contain(runId));
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_ShowRun_ShouldReturnRunDetails()
    {
        const string registryName = "topazacrrun01";
        const string resourceGroup = "test-acr-run-rg";
        const string taskName = "run-task";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");
        await RunAzureCliCommand(
            $"az acr task create --name {taskName} --registry {registryName} " +
            $"--resource-group {resourceGroup} --cmd \"echo hello\" --no-push --context /dev/null");

        string? runId = null;
        await RunAzureCliCommand(
            $"az acr task run --name {taskName} --registry {registryName} --resource-group {resourceGroup} --no-logs",
            resp => { runId = resp["runId"]?.GetValue<string>(); });

        await RunAzureCliCommand(
            $"az acr task show-run --run-id {runId} --registry {registryName} --resource-group {resourceGroup}",
            resp =>
            {
                Assert.That(resp["runId"]?.GetValue<string>(), Is.EqualTo(runId));
                Assert.That(resp["status"]?.GetValue<string>(), Is.EqualTo("Succeeded"));
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_UpdateRun_ShouldModifyArchiveEnabled()
    {
        const string registryName = "topazacrrun01";
        const string resourceGroup = "test-acr-run-rg";
        const string taskName = "run-task";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");
        await RunAzureCliCommand(
            $"az acr task create --name {taskName} --registry {registryName} " +
            $"--resource-group {resourceGroup} --cmd \"echo hello\" --no-push --context /dev/null");

        string? runId = null;
        await RunAzureCliCommand(
            $"az acr task run --name {taskName} --registry {registryName} --resource-group {resourceGroup} --no-logs",
            resp => { runId = resp["runId"]?.GetValue<string>(); });

        await RunAzureCliCommand(
            $"az acr task update-run --run-id {runId} --registry {registryName} --resource-group {resourceGroup} --no-archive false",
            resp =>
            {
                Assert.That(resp["runId"]?.GetValue<string>(), Is.EqualTo(runId));
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_ScheduleRun_ShouldReturnRun()
    {
        const string registryName = "topazacrrun01";
        const string resourceGroup = "test-acr-run-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");

        await RunAzureCliCommand(
            $"az rest --method post " +
            $"--url \"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/subscriptions/$(az account show --query id -o tsv)/resourceGroups/{resourceGroup}/providers/Microsoft.ContainerRegistry/registries/{registryName}/scheduleRun?api-version=2019-04-01\" " +
            $"--body '{{\"type\":\"DockerBuildRequest\",\"dockerFilePath\":\"Dockerfile\",\"platform\":{{\"os\":\"Linux\"}},\"isPushEnabled\":false}}'",
            resp =>
            {
                Assert.That(resp["properties"]?["runId"]?.GetValue<string>() ?? resp["runId"]?.GetValue<string>(), Is.Not.Null.And.Not.Empty);
                Assert.That(resp["properties"]?["status"]?.GetValue<string>() ?? resp["status"]?.GetValue<string>(), Is.EqualTo("Succeeded"));
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_GetLogContent_NonDockerRun_ReturnsFallbackLog()
    {
        const string registryName = "topazacrlogcontent01";
        const string resourceGroup = "test-acr-logcontent-rg";
        const string taskName = "log-task";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");
        await RunAzureCliCommand(
            $"az acr task create --name {taskName} --registry {registryName} " +
            $"--resource-group {resourceGroup} --cmd \"echo hello\" --no-push --context /dev/null");

        // Trigger a non-DockerBuildRequest run (TaskRunRequest) — immediate Succeeded.
        string? runId = null;
        await RunAzureCliCommand(
            $"az acr task run --name {taskName} --registry {registryName} --resource-group {resourceGroup} --no-logs",
            resp => { runId = resp["runId"]?.GetValue<string>(); });

        // Retrieve log SAS URL and fetch log content.
        string? logLink = null;
        await RunAzureCliCommand(
            $"az rest --method post " +
            $"--url \"https://topaz.local.dev:{GlobalSettings.DefaultResourceManagerPort}/subscriptions/$(az account show --query id -o tsv)/resourceGroups/{resourceGroup}/providers/Microsoft.ContainerRegistry/registries/{registryName}/runs/{runId}/listLogSasUrl?api-version=2019-04-01\"",
            resp => { logLink = resp["logLink"]?.GetValue<string>(); });

        Assert.That(logLink, Is.Not.Null.And.Not.Empty);

        // Fetch log content — just confirm the endpoint returns HTTP 200 (curl -sf fails on 4xx/5xx).
        await RunAzureCliCommand($"curl -sf \"{logLink}\"");

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

    [Test]
    public async Task AcrTaskRun_ScheduleRun_DockerBuildRequest_AzAcrRun_ShouldReturnRunId()
    {
        const string registryName = "topazacrdocker01";
        const string resourceGroup = "test-acr-docker-rg";

        await RunAzureCliCommand($"az group create -n {resourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {registryName} --resource-group {resourceGroup} --sku Standard --location westeurope");

        // az acr run calls scheduleRun with DockerBuildRequest.
        // In the test container Docker is not available, so Topaz falls back to immediate Succeeded.
        await RunAzureCliCommand(
            $"az acr run --registry {registryName} --resource-group {resourceGroup} " +
            $"--cmd \"echo hello\" /dev/null",
            resp =>
            {
                var runId = resp["runId"]?.GetValue<string>() ??
                            resp["properties"]?["runId"]?.GetValue<string>();
                Assert.That(runId, Is.Not.Null.And.Not.Empty);
            });

        await RunAzureCliCommand($"az acr delete --name {registryName} --resource-group {resourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {resourceGroup} --yes");
    }

}
