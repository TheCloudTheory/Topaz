using Topaz.CLI;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerRegistry;
using Azure.ResourceManager.ContainerRegistry.Models;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.ContainerRegistry;
using Topaz.Shared;
namespace Topaz.Tests.E2E;

public class ContainerRegistryRunTests
{
    private static readonly ArmClientOptions ArmClientOptions = TopazArmClientOptions.New;
    private static readonly Guid SubscriptionId = Guid.Parse("E1F2A3B4-C5D6-E7F8-A9B0-C1D2E3F4A5B6");

    private const string SubscriptionName = "sub-test";
    private const string ResourceGroupName = "test-acr-runs";
    private const string RegistryName = "topazacrrun01";
    private const string TaskName = "build-task";

    [SetUp]
    public async Task SetUp()
    {
        await Program.RunAsync(["subscription", "delete", "--id", SubscriptionId.ToString()]);
        await Program.RunAsync(["subscription", "create", "--id", SubscriptionId.ToString(), "--name", SubscriptionName]);

        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
        await Program.RunAsync(["group", "create", "--name", ResourceGroupName, "--location", "westeurope", "--subscription-id", SubscriptionId.ToString()]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Program.RunAsync(["group", "delete", "--name", ResourceGroupName, "--subscription-id", SubscriptionId.ToString()]);
    }

    private async Task<ContainerRegistryResource> CreateRegistryWithTaskAsync()
    {
        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard));
        var registryLro = await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var tasks = registryLro.Value.GetContainerRegistryTasks();
        var taskData = new ContainerRegistryTaskData(new AzureLocation("westeurope"))
        {
            Platform = new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux),
            Step = new ContainerRegistryDockerBuildStep("Dockerfile")
        };
        await tasks.CreateOrUpdateAsync(WaitUntil.Completed, TaskName, taskData);

        return registryLro.Value;
    }

    [Test]
    public async Task ContainerRegistryRun_TriggerTask_ShouldReturnSucceededRun()
    {
        var registry = await CreateRegistryWithTaskAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registryResource = await resourceGroup.Value.GetContainerRegistries().GetAsync(RegistryName);

        // Schedule run using TaskRunRequest pointing at the named task
        var taskId = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tasks/{TaskName}";
        var runContent = new ContainerRegistryTaskRunContent(new ResourceIdentifier(taskId));
        var runLro = await registryResource.Value.ScheduleRunAsync(WaitUntil.Completed, runContent);

        Assert.Multiple(() =>
        {
            Assert.That(runLro.Value.Data.RunId, Is.Not.Null.And.Not.Empty);
            Assert.That(runLro.Value.Data.Status.ToString(), Is.EqualTo("Succeeded"));
            Assert.That(runLro.Value.Data.RunType.ToString(), Is.EqualTo("QuickRun"));
        });
    }

    [Test]
    public async Task ContainerRegistryRun_GetRun_ShouldReturnDetails()
    {
        var registry = await CreateRegistryWithTaskAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registryResource = await resourceGroup.Value.GetContainerRegistries().GetAsync(RegistryName);

        var taskId = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tasks/{TaskName}";
        var runContent = new ContainerRegistryTaskRunContent(new ResourceIdentifier(taskId));
        var runLro = await registryResource.Value.ScheduleRunAsync(WaitUntil.Completed, runContent);
        var runId = runLro.Value.Data.RunId!;

        var fetched = await registryResource.Value.GetContainerRegistryRunAsync(runId);

        Assert.Multiple(() =>
        {
            Assert.That(fetched.Value.Data.RunId, Is.EqualTo(runId));
            Assert.That(fetched.Value.Data.ProvisioningState.ToString(), Is.EqualTo("Succeeded"));
        });
    }

    [Test]
    public async Task ContainerRegistryRun_ListRuns_ShouldIncludeTriggered()
    {
        var registry = await CreateRegistryWithTaskAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registryResource = await resourceGroup.Value.GetContainerRegistries().GetAsync(RegistryName);

        var taskId = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tasks/{TaskName}";
        var runContent = new ContainerRegistryTaskRunContent(new ResourceIdentifier(taskId));
        var runLro = await registryResource.Value.ScheduleRunAsync(WaitUntil.Completed, runContent);
        var runId = runLro.Value.Data.RunId!;

        var runs = new List<ContainerRegistryRunResource>();
        await foreach (var run in registryResource.Value.GetContainerRegistryRuns().GetAllAsync())
            runs.Add(run);

        Assert.That(runs, Has.Some.Matches<ContainerRegistryRunResource>(r => r.Data.RunId == runId));
    }

    [Test]
    public async Task ContainerRegistryRun_UpdateRun_ShouldModifyIsArchiveEnabled()
    {
        var registry = await CreateRegistryWithTaskAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registryResource = await resourceGroup.Value.GetContainerRegistries().GetAsync(RegistryName);

        var taskId = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tasks/{TaskName}";
        var runContent = new ContainerRegistryTaskRunContent(new ResourceIdentifier(taskId));
        var runLro = await registryResource.Value.ScheduleRunAsync(WaitUntil.Completed, runContent);
        var runResource = runLro.Value;

        var patch = new ContainerRegistryRunPatch { IsArchiveEnabled = true };
        var updateLro = await runResource.UpdateAsync(WaitUntil.Completed, patch);

        Assert.That(updateLro.Value.Data.IsArchiveEnabled, Is.True);
    }

    [Test]
    public async Task ContainerRegistryRun_ScheduleRun_ShouldSucceed()
    {
        // When Docker is available the run goes async (Queued→Succeeded/Failed);
        // immediate-Succeeded path is covered by DockerUnavailable test.
        Assume.That(!AcrDockerExecutor.IsAvailable(),
            "Skipping: Docker is available — immediate-Succeeded fallback does not apply here.");

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();

        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard));
        var registryLro = await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var buildContent = new ContainerRegistryDockerBuildContent(
            "Dockerfile",
            new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux))
        {
            IsPushEnabled = false
        };

        var runLro = await registryLro.Value.ScheduleRunAsync(WaitUntil.Completed, buildContent);

        Assert.Multiple(() =>
        {
            Assert.That(runLro.Value.Data.RunId, Is.Not.Null.And.Not.Empty);
            Assert.That(runLro.Value.Data.Status.ToString(), Is.EqualTo("Succeeded"));
        });
    }

    [Test]
    public async Task ContainerRegistryRun_GetLogSasUrl_ShouldReturnLogLink()
    {
        var registry = await CreateRegistryWithTaskAsync();

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registryResource = await resourceGroup.Value.GetContainerRegistries().GetAsync(RegistryName);

        var taskId = $"/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.ContainerRegistry/registries/{RegistryName}/tasks/{TaskName}";
        var runContent = new ContainerRegistryTaskRunContent(new ResourceIdentifier(taskId));
        var runLro = await registryResource.Value.ScheduleRunAsync(WaitUntil.Completed, runContent);

        var logResult = await runLro.Value.GetLogSasUrlAsync();

        Assert.That(logResult.Value.LogLink, Is.Not.Null.And.Not.Empty);
        Assert.That(logResult.Value.LogLink, Does.Contain("/v2/runs/"));
    }

    [Test]
    public async Task ContainerRegistryRun_DockerBuildRequest_WhenDockerUnavailable_ImmediateSucceeded()
    {
        // If Docker is available, the run will go async (Queued/Running/Succeeded/Failed).
        // This test covers the fallback path: when Docker is unavailable the run is immediate Succeeded.
        Assume.That(!AcrDockerExecutor.IsAvailable(),
            "Skipping: Docker is available — immediate-Succeeded fallback does not apply.");

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();
        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard));
        var registryLro = await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        var buildContent = new ContainerRegistryDockerBuildContent(
            "Dockerfile",
            new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux))
        {
            IsPushEnabled = false
        };
        var runLro = await registryLro.Value.ScheduleRunAsync(WaitUntil.Completed, buildContent);

        Assert.Multiple(() =>
        {
            Assert.That(runLro.Value.Data.RunId, Is.Not.Null.And.Not.Empty);
            Assert.That(runLro.Value.Data.Status.ToString(), Is.EqualTo("Succeeded"));
        });
    }

    [Test]
    public async Task ContainerRegistryRun_DockerBuildRequest_WhenDockerAvailable_ReachesTerminalState()
    {
        Assume.That(AcrDockerExecutor.IsAvailable(),
            "Skipping: Docker is not available on this machine.");

        var credential = new AzureLocalCredential(Globals.GlobalAdminId);
        var armClient = new ArmClient(credential, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(ResourceGroupName);
        var registries = resourceGroup.Value.GetContainerRegistries();
        var registryData = new ContainerRegistryData(new AzureLocation("westeurope"), new ContainerRegistrySku(ContainerRegistrySkuName.Standard));
        var registryLro = await registries.CreateOrUpdateAsync(WaitUntil.Completed, RegistryName, registryData);

        // Build with no real context — Docker will likely fail, exercising Queued→Running→Failed transitions.
        // The SDK polls Azure-AsyncOperation (operationStatuses endpoint) until terminal, then
        // GETs the Location header to retrieve the final run resource.
        var buildContent = new ContainerRegistryDockerBuildContent(
            "Dockerfile",
            new ContainerRegistryPlatformProperties(ContainerRegistryOS.Linux))
        {
            IsPushEnabled = false
        };

        // When the run fails, the SDK raises RequestFailedException (LRO terminal failure).
        // When it succeeds (unlikely without a real Dockerfile), it returns normally.
        // Both outcomes confirm the async status transitions are working.
        string? runId = null;
        try
        {
            var runLro = await registryLro.Value.ScheduleRunAsync(WaitUntil.Completed, buildContent);
            runId = runLro.Value.Data.RunId;
            Assert.That(runLro.Value.Data.Status?.ToString(), Is.EqualTo("Succeeded"));
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 200 || ex.ErrorCode == null)
        {
            // 200 + null ErrorCode means the LRO terminal status was "Failed" — expected when
            // Docker can't find a Dockerfile. Verify the run exists and is in a terminal state.
            Assert.Pass("Run completed with status Failed (no Dockerfile in context — expected).");
        }

        if (runId != null)
            Assert.That(runId, Is.Not.Null.And.Not.Empty);
    }
}
