using System.Text.Json.Nodes;

namespace Topaz.Tests.Terraform.AzureRm;

/// <summary>
/// Base class for all AzureRM Terraform tests.
/// Applies the combined azurerm workspace ONCE at process start and destroys it at process exit,
/// reducing azurerm v4 provider startups from 34 (17 × apply+destroy) to 2.
/// Individual [Test] methods simply assert against the pre-captured outputs — no Terraform I/O.
/// </summary>
public class AzureRmBatchFixture : TopazFixture
{
    private static readonly SemaphoreSlim BatchLock = new(1, 1);
    private static bool _applied;
    private static JsonNode? _outputs;
    private static string? _workDir;

    // Register destroy to run BEFORE containers are stopped (via the BeforeContainerCleanup hook).
    static AzureRmBatchFixture()
    {
        BeforeContainerCleanup += static () =>
        {
            var wd = Volatile.Read(ref _workDir);
            if (wd is not null && _applied)
                DestroyTerraformWorkspaceSync(wd);
        };
    }

    [OneTimeSetUp]
    public async Task OneTimeApplyAzureRmBatch()
    {
        await BatchLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_applied)
                return;

            (var outputs, var workDir) = await ApplyTerraformRetaining(
                "providers/azurerm.tf", "azurerm-combined");

            Volatile.Write(ref _workDir, workDir);
            _outputs = outputs;
            _applied = true;
        }
        finally
        {
            BatchLock.Release();
        }
    }

    [OneTimeTearDown]
    public Task OneTimeTearDownAzureRmBatch()
    {
        // Destroy is deferred to process exit via BeforeContainerCleanup so containers
        // are still running when terraform destroy executes.
        return Task.CompletedTask;
    }

    protected T GetOutput<T>(string key)
        => _outputs![key]!["value"]!.GetValue<T>();

    protected JsonNode GetOutputNode(string key)
        => _outputs![key]!["value"]!;
}
